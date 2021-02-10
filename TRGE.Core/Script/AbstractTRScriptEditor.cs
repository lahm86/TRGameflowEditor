﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TRGE.Core
{
    public abstract class AbstractTRScriptEditor
    {
        protected TRScriptIOArgs _io;

        internal FileInfo OriginalFile
        {
            get => _io.OriginalFile;
            set => _io.OriginalFile = value;
        }

        internal FileInfo BackupFile
        {
            get => _io.BackupFile;
            set => _io.BackupFile = value;
        }

        internal FileInfo ConfigFile
        {
            get => _io.ConfigFile;
            set => _io.ConfigFile = value;
        }

        internal DirectoryInfo OutputDirectory
        {
            get => _io.OutputDirectory;
            set => _io.OutputDirectory = value;
        }

        public TREdition Edition => Script.Edition;
        internal bool AllowSuccessiveEdits { get; set; }

        internal AbstractTRLevelManager LevelManager { get; private set; }
        internal AbstractTRFrontEnd FrontEnd => Script.FrontEnd;
        internal AbstractTRScript Script { get; private set; }

        protected TRScriptOpenOption _openOption;
        protected Dictionary<string, object> _config;

        internal event EventHandler<TRScriptedLevelEventArgs> LevelModified;

        internal AbstractTRScriptEditor(TRScriptIOArgs ioArgs, TRScriptOpenOption openOption)
        {
            _io = ioArgs;
            _openOption = openOption;
            Initialise();
        }

        internal void Initialise()
        {
            Initialise(CreateScript());
        }

        private void Initialise(AbstractTRScript script)
        {
            LoadConfig();

            (Script = script).Read(BackupFile);

            LevelManager = TRScriptedLevelFactory.GetLevelManager(script);
            LevelManager.LevelModified += LevelManagerLevelModified;

            ReadConfig();
        }

        private void LevelManagerLevelModified(object sender, TRScriptedLevelEventArgs e)
        {
            LevelModified?.Invoke(this, e);
        }

        private void LoadConfig()
        {
            _config = ConfigFile.Exists ? JsonConvert.DeserializeObject<Dictionary<string, object>>(ConfigFile.ReadCompressedText()) : null;
            //issue #36
            if (_config != null && !OriginalFile.Checksum().Equals(_config["CheckSumOnSave"]))
            {
                switch (_openOption)
                {
                    //callers should make a different OpenOption and try to open the script again
                    case TRScriptOpenOption.Default:
                        throw new ChecksumMismatchException();
                    //overwrite the existing backup with the "new" original file, delete the config as though we have never opened the file before
                    case TRScriptOpenOption.DiscardBackup:
                        OriginalFile.CopyTo(BackupFile.FullName, true);
                        ConfigFile.Delete();
                        break;
                    //overwrite the original file with the backup
                    case TRScriptOpenOption.RestoreBackup:
                        BackupFile.CopyTo(OriginalFile.FullName, true);
                        break;
                }

                _openOption = TRScriptOpenOption.Default;
            }
        }

        protected void ReadConfig()
        {
            if (_config != null)
            {
                Dictionary<string, object> levelSeq = JsonConvert.DeserializeObject<Dictionary<string, object>>(_config["LevelSequencing"].ToString());
                LevelSequencingOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), levelSeq["Organisation"]);
                LevelSequencingRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(levelSeq["RNG"].ToString()));
                //note that even if the levels were randomised, this would have been done after saving the config file
                //so reloading the sequencing will either just restore defaults or set it to a manual sequence if the user
                //picked that at one point in the previous edit
                LevelSequencing = JsonConvert.DeserializeObject<List<Tuple<string, string>>>(levelSeq["Data"].ToString());

                Dictionary<string, object> trackInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(_config["GameTracks"].ToString());
                GameTrackOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), trackInfo["Organisation"]);
                GameTrackRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(trackInfo["RNG"].ToString()));
                //see note above
                GameTrackData = JsonConvert.DeserializeObject<List<MutableTuple<string, string, ushort>>>(trackInfo["Data"].ToString());

                Dictionary<string, object> secretInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(_config["SecretSupport"].ToString());
                LevelSecretSupportOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), secretInfo["Organisation"]);
                LevelSecretSupport = JsonConvert.DeserializeObject<List<MutableTuple<string, string, bool>>>(secretInfo["Data"].ToString());

                Dictionary<string, object> sunsetInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(_config["Sunsets"].ToString());
                LevelSunsetOrganisation = (Organisation)Enum.ToObject(typeof(Organisation), sunsetInfo["Organisation"]);
                LevelSunsetRNG = new RandomGenerator(JsonConvert.DeserializeObject<Dictionary<string, object>>(sunsetInfo["RNG"].ToString()));
                //see note above
                LevelSunsetData = JsonConvert.DeserializeObject<List<MutableTuple<string, string, bool>>>(sunsetInfo["Data"].ToString());
                RandomSunsetLevelCount = uint.Parse(sunsetInfo["RandomCount"].ToString());

                FrontEndHasFMV = bool.Parse(_config["FrontEndFMVOn"].ToString());
                AllowSuccessiveEdits = bool.Parse(_config["Successive"].ToString());
            }
            else
            {
                LevelSequencingOrganisation = Organisation.Default;
                LevelSequencingRNG = new RandomGenerator(RandomGenerator.Type.Date);
                GameTrackOrganisation = Organisation.Default;
                GameTrackRNG = new RandomGenerator(RandomGenerator.Type.Date);
                LevelSecretSupportOrganisation = Organisation.Default;
                LevelSunsetOrganisation = Organisation.Default;
                LevelSunsetRNG = new RandomGenerator(RandomGenerator.Type.Date);
                RandomSunsetLevelCount = LevelManager.GetSunsetLevelCount();

                AllowSuccessiveEdits = false;
            }

            ApplyConfig();
        }

        internal void Save(TRSaveMonitor monitor)
        {
            monitor.FireSaveStateChanged(0, TRSaveCategory.Scripting);

            _config = new Dictionary<string, object>
            {
                ["Version"] = Assembly.GetExecutingAssembly().GetName().Version,
                ["Original"] = OriginalFile.FullName,
                ["CheckSumOnSave"] = string.Empty,
                ["FrontEndFMVOn"] = FrontEndHasFMV,
                ["Successive"] = AllowSuccessiveEdits,
                ["LevelSequencing"] = new Dictionary<string, object>
                {
                    ["Organisation"] = (int)LevelSequencingOrganisation,
                    ["RNG"] = LevelSequencingRNG.ToJson(),
                    ["Data"] = LevelSequencing
                },
                ["GameTracks"] = new Dictionary<string, object>
                {
                    ["Organisation"] = (int)GameTrackOrganisation,
                    ["RNG"] = GameTrackRNG.ToJson(),
                    ["Data"] = GameTrackData
                },
                ["SecretSupport"] = new Dictionary<string, object>
                {
                    ["Organisation"] = (int)LevelSecretSupportOrganisation,
                    ["Data"] = LevelSecretSupport
                },
                ["Sunsets"] = new Dictionary<string, object>
                {
                    ["Organisation"] = (int)LevelSunsetOrganisation,
                    ["RNG"] = LevelSunsetRNG.ToJson(),
                    ["RandomCount"] = RandomSunsetLevelCount,
                    ["Data"] = LevelSunsetData
                }
            };

            AbstractTRScript backupScript = LoadBackupScript();
            AbstractTRScript randoBaseScript = LoadRandomisationBaseScript(); // #42

            if (LevelSequencingOrganisation == Organisation.Random)
            {
                LevelManager.RandomiseSequencing(randoBaseScript.Levels);
            }
            else if (LevelSequencingOrganisation == Organisation.Default)
            {
                LevelManager.RestoreSequencing(backupScript.Levels);
            }

            if (GameTrackOrganisation == Organisation.Random)
            {
                LevelManager.RandomiseGameTracks(randoBaseScript.Levels);
            }
            else if (GameTrackOrganisation == Organisation.Default)
            {
                LevelManager.RestoreGameTracks(backupScript);
            }

            if (LevelSecretSupportOrganisation == Organisation.Default)
            {
                LevelManager.RestoreSecretSupport(backupScript.Levels);
            }

            if (LevelSunsetOrganisation == Organisation.Random)
            {
                LevelManager.RandomiseSunsets(randoBaseScript.Levels);
            }
            else if (LevelSunsetOrganisation == Organisation.Default)
            {
                LevelManager.RestoreSunsetData(backupScript.Levels);
            }
            else
            {
                LevelManager.SetSunsetData(LevelSunsetData); //TODO: Fix this - it's in place to ensure the event is triggered for any listeners
            }

            SaveImpl();
            LevelManager.Save();

            string outputPath = GetScriptOutputPath();
            Script.Write(outputPath);

            _config["CheckSumOnSave"] = new FileInfo(outputPath).Checksum();

            ConfigFile.WriteCompressedText(JsonConvert.SerializeObject(_config, Formatting.None)); //#48

            monitor.FireSaveStateChanged(1);
        }

        public void Restore()
        {
            BackupFile.CopyTo(OriginalFile.FullName, true);
            while (File.Exists(ConfigFile.FullName))
            {
                ConfigFile.Delete(); //issue #39
            }
            ConfigFile = new FileInfo(ConfigFile.FullName);
            Initialise(Script);
        }

        protected string GetScriptOutputPath()
        {
            return Path.Combine(OutputDirectory.FullName, OriginalFile.Name);
        }

        internal AbstractTRScript LoadBackupScript()
        {
            return LoadScript(BackupFile.FullName);
        }

        internal AbstractTRScript LoadOutputScript()
        {
            return LoadScript(GetScriptOutputPath());
        }

        internal AbstractTRScript LoadRandomisationBaseScript()
        {
            return AllowSuccessiveEdits ? LoadOutputScript() : LoadBackupScript();
        }

        internal AbstractTRScript LoadScript(string filePath)
        {
            AbstractTRScript script = CreateScript();
            script.Read(filePath);
            return script;
        }

        internal List<AbstractTRScriptedLevel> GetOriginalLevels()
        {
            return LoadBackupScript().Levels;
        }

        internal abstract AbstractTRScript CreateScript();

        protected abstract void SaveImpl();
        protected abstract void ApplyConfig();

        public Organisation LevelSequencingOrganisation
        {
            get => LevelManager.SequencingOrganisation;
            set => LevelManager.SequencingOrganisation = value;
        }

        public RandomGenerator LevelSequencingRNG
        {
            get => LevelManager.SequencingRNG;
            set => LevelManager.SequencingRNG = value;
        }

        public List<Tuple<string, string>> LevelSequencing
        {
            get => LevelManager.GetSequencing();
            set => LevelManager.SetSequencing(value);
        }

        internal void RandomiseLevels()
        {
            LevelManager.RandomiseSequencing(LoadBackupScript().Levels);
        }

        public bool FrontEndHasFMV
        {
            get => FrontEnd.HasFMV;
            set => FrontEnd.HasFMV = value;
        }

        public Organisation GameTrackOrganisation
        {
            get => LevelManager.GameTrackOrganisation;
            set => LevelManager.GameTrackOrganisation = value;
        }

        public RandomGenerator GameTrackRNG
        {
            get => LevelManager.GameTrackRNG;
            set => LevelManager.GameTrackRNG = value;
        }

        public virtual List<MutableTuple<string, string, ushort>> GameTrackData
        {
            get => LevelManager.GetTrackData(LoadBackupScript().Levels);
            set => LevelManager.SetTrackData(value);
        }

        public IReadOnlyList<Tuple<ushort, string>> AllGameTracks => LevelManager.GetAllGameTracks();

        internal TRAudioTrack TitleTrack => LevelManager.AudioProvider.GetTrack(Script.TitleSoundID);
        internal TRAudioTrack SecretTrack => LevelManager.AudioProvider.GetTrack(Script.SecretSoundID);

        public byte[] GetTrackData(ushort trackID)
        {
            return LevelManager.AudioProvider.GetTrackData(trackID);
        }

        internal void RandomiseGameTracks()
        {
            LevelManager.RandomiseGameTracks(LoadBackupScript().Levels);
        }

        public Organisation LevelSecretSupportOrganisation
        {
            get => LevelManager.SecretSupportOrganisation;
            set
            {
                if (value == Organisation.Random)
                {
                    throw new ArgumentException("Randomisation of level secret support is not implemented.");
                }
                LevelManager.SecretSupportOrganisation = value;
            }
        }

        public List<MutableTuple<string, string, bool>> LevelSecretSupport
        {
            get => LevelManager.GetSecretSupport(LoadBackupScript().Levels);
            set => LevelManager.SetSecretSupport(value);
        }

        public bool CanSetSunsets => LevelManager.CanSetSunsets;

        public Organisation LevelSunsetOrganisation
        {
            get => LevelManager.SunsetOrganisation;
            set => LevelManager.SunsetOrganisation = value;
        }

        public RandomGenerator LevelSunsetRNG
        {
            get => LevelManager.SunsetRNG;
            set => LevelManager.SunsetRNG = value;
        }

        public uint RandomSunsetLevelCount
        {
            get => LevelManager.RandomSunsetCount;
            set => LevelManager.RandomSunsetCount = value;
        }

        public List<MutableTuple<string, string, bool>> LevelSunsetData
        {
            get => LevelManager.GetSunsetData(LoadBackupScript().Levels);
            set => LevelManager.SetSunsetData(value);
        }
    }
}