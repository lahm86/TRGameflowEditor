﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TRGE.Core
{
    public class TR23ScriptEditor : AbstractTRScriptEditor
    {
        internal TR23ScriptEditor(TRScriptIOArgs ioArgs, TRScriptOpenOption openOption)
            : base(ioArgs, openOption) { }        

        protected override void ApplyConfig(Dictionary<string, object> config)
        {
            if (config == null)
            {
                UnarmedLevelOrganisation = Organisation.Default;
                UnarmedLevelRNG = new RandomGenerator(RandomGenerator.Type.Date);
                RandomUnarmedLevelCount = Math.Max(1, (LevelManager as TR23LevelManager).GetUnarmedLevelCount());

                AmmolessLevelOrganisation = Organisation.Default;
                AmmolessLevelRNG = new RandomGenerator(RandomGenerator.Type.Date);
                RandomAmmolessLevelCount = Math.Max(1, (LevelManager as TR23LevelManager).GetAmmolessLevelCount());

                SecretBonusOrganisation = Organisation.Default;
                SecretBonusRNG = new RandomGenerator(RandomGenerator.Type.Date);

                return;
            }

            Dictionary<string, object> unarmedLevels = JsonConvert.DeserializeObject<Dictionary<string, object>>(config["UnarmedLevels"].ToString());
            UnarmedLevelOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), unarmedLevels["Organisation"]);
            UnarmedLevelRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(unarmedLevels["RNG"].ToString()));
            RandomUnarmedLevelCount = uint.Parse(unarmedLevels["RandomCount"].ToString());
            //see note in base.LoadConfig re restoring randomised - same applies for Unarmed
            UnarmedLevelData = JsonConvert.DeserializeObject<List<MutableTuple<string, string, bool>>>(unarmedLevels["Data"].ToString());

            Dictionary<string, object> ammolessLevels = JsonConvert.DeserializeObject<Dictionary<string, object>>(config["AmmolessLevels"].ToString());
            AmmolessLevelOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), ammolessLevels["Organisation"]);
            AmmolessLevelRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(ammolessLevels["RNG"].ToString()));
            RandomAmmolessLevelCount = uint.Parse(ammolessLevels["RandomCount"].ToString());
            //see note in base.LoadConfig re restoring randomised - same applies for Ammoless
            AmmolessLevelData = JsonConvert.DeserializeObject<List<MutableTuple<string, string, bool>>>(ammolessLevels["Data"].ToString());

            if (CanOrganiseBonuses)
            {
                Dictionary<string, object> bonuses = JsonConvert.DeserializeObject<Dictionary<string, object>>(config["BonusSetup"].ToString());
                SecretBonusOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), bonuses["Organisation"]);
                SecretBonusRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(bonuses["RNG"].ToString()));
                LevelSecretBonusData = JsonConvert.DeserializeObject<List<MutableTuple<string, string, List<MutableTuple<ushort, TRItemCategory, string, int>>>>>(bonuses["Data"].ToString());
            }

            LevelsHaveCutScenes = bool.Parse(config["LevelCutScenesOn"].ToString());
            LevelsHaveFMV = bool.Parse(config["LevelFMVsOn"].ToString());
            LevelsHaveStartAnimation = bool.Parse(config["LevelStartAnimsOn"].ToString());
            CheatsEnabled = bool.Parse(config["CheatsOn"].ToString());
            DemosEnabled = bool.Parse(config["DemosOn"].ToString());
            DemoTime = uint.Parse(config["DemoTime"].ToString());
            IsDemoVersion = bool.Parse(config["DemoVersion"].ToString());
            DozyEnabled = bool.Parse(config["DozyOn"].ToString());
            GymEnabled = bool.Parse(config["GymOn"].ToString());
            LevelSelectEnabled = bool.Parse(config["LevelSelectOn"].ToString());
            OptionRingEnabled = bool.Parse(config["OptionRingOn"].ToString());
            SaveLoadEnabled = bool.Parse(config["SaveLoadOn"].ToString());
            ScreensizingEnabled = bool.Parse(config["ScreensizeOn"].ToString());
            TitleScreenEnabled = bool.Parse(config["TitlesOn"].ToString());
        }

        protected override void SaveImpl()
        {
            //for unarmed, ammoless and bonus data, save the config before
            //running any randomisation, otherwise when the script is
            //reloaded the random order will be available
            _config["UnarmedLevels"] = new Dictionary<string, object>
            {
                ["Organisation"] = (int)UnarmedLevelOrganisation,
                ["RNG"] = UnarmedLevelRNG.ToJson(),
                ["RandomCount"] = RandomUnarmedLevelCount,
                ["Data"] = UnarmedLevelData
            };

            _config["AmmolessLevels"] = new Dictionary<string, object>
            {
                ["Organisation"] = (int)AmmolessLevelOrganisation,
                ["RNG"] = AmmolessLevelRNG.ToJson(),
                ["RandomCount"] = RandomAmmolessLevelCount,
                ["Data"] = AmmolessLevelData
            };

            if (CanOrganiseBonuses)
            {
                _config["BonusSetup"] = new Dictionary<string, object>
                {
                    ["Organisation"] = (int)SecretBonusOrganisation,
                    ["RNG"] = SecretBonusRNG.ToJson(),
                    ["Data"] = LevelSecretBonusData
                };
            }

            _config["LevelCutScenesOn"] = LevelsHaveCutScenes;
            _config["LevelFMVsOn"] = LevelsHaveFMV;
            _config["LevelStartAnimsOn"] = LevelsHaveStartAnimation;
            _config["CheatsOn"] = CheatsEnabled;
            _config["DemosOn"] = DemosEnabled;
            _config["DemoTime"] = DemoTime;
            _config["DemoVersion"] = IsDemoVersion;
            _config["DozyOn"] = DozyEnabled;
            _config["GymOn"] = GymEnabled;
            _config["LevelSelectOn"] = LevelSelectEnabled;
            _config["OptionRingOn"] = OptionRingEnabled;
            _config["SaveLoadOn"] = SaveLoadEnabled;
            _config["ScreensizeOn"] = ScreensizingEnabled;
            _config["TitlesOn"] = TitleScreenEnabled;

            AbstractTRScript backupScript = LoadBackupScript();
            AbstractTRScript randoBaseScript = LoadRandomisationBaseScript(); // #42

            List<AbstractTRScriptedLevel> backupLevels = backupScript.Levels;
            List<AbstractTRScriptedLevel> randoBaseLevels = randoBaseScript.Levels;
            TR23LevelManager originalLevelManager = TRScriptedLevelFactory.GetLevelManager(LoadScript(OriginalFile.FullName)) as TR23LevelManager; //#65
            TR23LevelManager currentLevelManager = LevelManager as TR23LevelManager;
            
            if (AmmolessLevelOrganisation == Organisation.Random)
            {
                if (TRInterop.RandomisationSupported)
                {
                    currentLevelManager.RandomiseAmmolessLevels(randoBaseLevels);
                }
                else
                {
                    currentLevelManager.SetAmmolessLevelData(originalLevelManager.GetAmmolessLevelData(originalLevelManager.Levels)); //#65 lock to that of the original file
                }
            }
            else if (AmmolessLevelOrganisation == Organisation.Default)
            {
                currentLevelManager.RestoreAmmolessLevels(backupLevels);
            }

            if (UnarmedLevelOrganisation == Organisation.Random)
            {
                if (TRInterop.RandomisationSupported)
                {
                    currentLevelManager.RandomiseUnarmedLevels(randoBaseLevels);
                }
                else
                {
                    currentLevelManager.SetUnarmedLevelData(originalLevelManager.GetUnarmedLevelData(originalLevelManager.Levels)); //#65 lock to that of the original file
                }
            }
            else if (UnarmedLevelOrganisation == Organisation.Default)
            {
                currentLevelManager.RestoreUnarmedLevels(backupLevels);
            }
            else
            {
                currentLevelManager.SetUnarmedLevelData(UnarmedLevelData); //TODO: Fix this - it's in place to ensure the event is triggered for any listeners
            }

            //should occur after unarmed organisation and does not use any other script
            //as basis, as order of levels is essential
            if (SecretBonusOrganisation == Organisation.Random)
            {
                if (TRInterop.RandomisationSupported)
                {
                    currentLevelManager.RandomiseBonuses();
                }
                else
                {
                    currentLevelManager.SetLevelBonusData(originalLevelManager.GetLevelBonusData(originalLevelManager.Levels)); //#65 lock to that of the original file
                }
            }
            else if (SecretBonusOrganisation == Organisation.Default)
            {
                currentLevelManager.RestoreBonuses(backupLevels);
            }
        }

        internal override AbstractTRScript CreateScript()
        {
            return new TR23Script();
        }

        /// <summary>
        /// A list of Tuples configured as follows:
        ///     Item1 => Level ID (string);
        ///     Item2 => Level Name (string);
        ///     Item3 => Is unarmed (bool).
        /// </summary>
        public List<MutableTuple<string, string, bool>> UnarmedLevelData
        {
            get => (LevelManager as TR23LevelManager).GetUnarmedLevelData(LoadBackupScript().Levels);
            set => (LevelManager as TR23LevelManager).SetUnarmedLevelData(value);
        }

        public Organisation UnarmedLevelOrganisation
        {
            get => (LevelManager as TR23LevelManager).UnarmedLevelOrganisation;
            set => (LevelManager as TR23LevelManager).UnarmedLevelOrganisation = value;
        }

        public RandomGenerator UnarmedLevelRNG
        {
            get => (LevelManager as TR23LevelManager).UnarmedLevelRNG;
            set => (LevelManager as TR23LevelManager).UnarmedLevelRNG = value;
        }

        public uint RandomUnarmedLevelCount
        {
            get => (LevelManager as TR23LevelManager).RandomUnarmedLevelCount;
            set => (LevelManager as TR23LevelManager).RandomUnarmedLevelCount = value;
        }

        internal void RandomiseUnarmedLevels()
        {
            (LevelManager as TR23LevelManager).RandomiseUnarmedLevels(LoadRandomisationBaseScript().Levels);
        }

        internal List<TR23ScriptedLevel> GetUnarmedLevels()
        {
            return (LevelManager as TR23LevelManager).GetUnarmedLevels();
        }

        /// <summary>
        /// A list of Tuples configured as follows:
        ///     Item1 => Level ID (string);
        ///     Item2 => Level Name (string);
        ///     Item3 => Ammo removed (bool).
        /// </summary>
        public List<MutableTuple<string, string, bool>> AmmolessLevelData
        {
            get => (LevelManager as TR23LevelManager).GetAmmolessLevelData(LoadBackupScript().Levels);
            set => (LevelManager as TR23LevelManager).SetAmmolessLevelData(value);
        }

        public Organisation AmmolessLevelOrganisation
        {
            get => (LevelManager as TR23LevelManager).AmmolessLevelOrganisation;
            set => (LevelManager as TR23LevelManager).AmmolessLevelOrganisation = value;
        }

        public RandomGenerator AmmolessLevelRNG
        {
            get => (LevelManager as TR23LevelManager).AmmolessLevelRNG;
            set => (LevelManager as TR23LevelManager).AmmolessLevelRNG = value;
        }

        public uint RandomAmmolessLevelCount
        {
            get => (LevelManager as TR23LevelManager).RandomAmmolessLevelCount;
            set => (LevelManager as TR23LevelManager).RandomAmmolessLevelCount = value;
        }

        internal void RandomiseAmmolessLevels()
        {
            (LevelManager as TR23LevelManager).RandomiseAmmolessLevels(LoadRandomisationBaseScript().Levels);
        }

        internal List<TR23ScriptedLevel> GetAmmolessLevels()
        {
            return (LevelManager as TR23LevelManager).GetAmmolessLevels();
        }

        public bool CanOrganiseBonuses => (LevelManager as TR23LevelManager).CanOrganiseBonuses;
        public Organisation SecretBonusOrganisation
        {
            get => (LevelManager as TR23LevelManager).BonusOrganisation;
            set => (LevelManager as TR23LevelManager).BonusOrganisation = value;
        }

        public RandomGenerator SecretBonusRNG
        {
            get => (LevelManager as TR23LevelManager).BonusRNG;
            set => (LevelManager as TR23LevelManager).BonusRNG = value;
        }

        public List<MutableTuple<string, string, List<MutableTuple<ushort, TRItemCategory, string, int>>>> LevelSecretBonusData
        {
            get
            {
                if (CanOrganiseBonuses)
                {
                    return (LevelManager as TR23LevelManager).GetLevelBonusData(LoadBackupScript().Levels);
                }
                return null;
            }
            set
            {
                if (CanOrganiseBonuses)
                {
                    (LevelManager as TR23LevelManager).SetLevelBonusData(value);
                }
            }
        }

        internal void RandomiseBonuses()
        {
            (LevelManager as TR23LevelManager).RandomiseBonuses(/*LoadBackupScript().Levels*/);
        }

        public bool LevelsHaveCutScenes
        {
            get => (LevelManager as TR23LevelManager).GetLevelsHaveCutScenes();
            set => (LevelManager as TR23LevelManager).SetLevelsHaveCutScenes(value);
        }

        public bool LevelsSupportCutScenes => (LevelManager as TR23LevelManager).GetLevelsSupportCutScenes();

        public bool LevelsHaveFMV
        {
            get => (LevelManager as TR23LevelManager).GetLevelsHaveFMV();
            set => (LevelManager as TR23LevelManager).SetLevelsHaveFMV(value);
        }

        public bool LevelsSupportFMVs => (LevelManager as TR23LevelManager).GetLevelsSupportFMVs();

        public bool LevelsHaveStartAnimation
        {
            get => (LevelManager as TR23LevelManager).GetLevelsHaveStartAnimation();
            set => (LevelManager as TR23LevelManager).SetLevelsHaveStartAnimation(value);
        }

        public bool LevelsSupportStartAnimations => (LevelManager as TR23LevelManager).GetLevelsSupportStartAnimations();

        public bool CheatsEnabled
        {
            get => !(Script as TR23Script).CheatsIgnored;
            set => (Script as TR23Script).CheatsIgnored = !value;
        }

        public bool DemosEnabled
        {
            get => !(Script as TR23Script).DemosDisabled;
            set => (Script as TR23Script).DemosDisabled = !value;
        }

        public uint DemoTime
        {
            get => (Script as TR23Script).DemoTimeSeconds;
            set => (Script as TR23Script).DemoTimeSeconds = value;
        }

        public bool IsDemoVersion
        {
            get => (Script as TR23Script).DemoVersion;
            set => (Script as TR23Script).DemoVersion = value;
        }

        public bool DozyEnabled
        {
            get => (Script as TR23Script).DozyEnabled;
            set => (Script as TR23Script).DozyEnabled = value;
        }

        public bool DozySupported => (Script as TR23Script).DozyViable;

        public bool GymEnabled
        {
            get => (Script as TR23Script).GymEnabled;
            set => (Script as TR23Script).GymEnabled = value;
        }

        public bool LevelSelectEnabled
        {
            get => (Script as TR23Script).LevelSelectEnabled;
            set => (Script as TR23Script).LevelSelectEnabled = value;
        }

        public bool OptionRingEnabled
        {
            get => !(Script as TR23Script).OptionRingDisabled;
            set => (Script as TR23Script).OptionRingDisabled = !value;
        }

        public bool SaveLoadEnabled
        {
            get => !(Script as TR23Script).SaveLoadDisabled;
            set => (Script as TR23Script).SaveLoadDisabled = !value;
        }

        public bool ScreensizingEnabled
        {
            get => !(Script as TR23Script).ScreensizingDisabled;
            set => (Script as TR23Script).ScreensizingDisabled = !value;
        }

        public bool TitleScreenEnabled
        {
            get => !(Script as TR23Script).TitleDisabled;
            set => (Script as TR23Script).TitleDisabled = !value;
        }
    }
}