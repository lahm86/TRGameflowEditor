﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TRGE.Core
{
    internal class TR23Script : AbstractTRScript
    {
        internal const uint Version = 3;

        #region Script Variables
        private string _description;
        internal string Description
        {
            get => _description;
            set
            {
                if (value.Length > 256)
                {
                    throw new IndexOutOfRangeException();
                }
                _description = value;
            }
        }

        internal ushort GameflowSize { get; private set; }
        internal uint FirstOption { get; private set; }
        internal int TitleReplace { get; private set; }
        internal uint DeathDemoMode { get; private set; }
        internal uint DeathInGame { get; private set; }
        internal uint DemoTime { get; private set; }
        internal uint DemoTimeSeconds
        {
            get => DemoTime / 30;
            set => DemoTime = value * 30;
        }
        internal uint DemoInterrupt { get; private set; }
        internal uint DemoEnd { get; private set; }       
        internal ushort NumLevels { get; private set; }
        internal ushort NumPlayableLevels => (ushort)(NumLevels - NumDemoLevels);
        internal ushort NumPictures { get; private set; }
        internal ushort NumTitles { get; private set; }
        internal ushort NumRPLs { get; private set; }
        internal ushort NumCutScenes { get; private set; }
        internal ushort NumDemoLevels { get; private set; }
        internal override ushort TitleSoundID { get; set; }
        internal ushort SingleLevel { get; private set; }        
        internal ushort Flags { get; private set; }        
        internal byte Xor { get; private set; }
        internal byte Language { get; private set; }
        internal byte SecretSound { get; private set; }
        internal override ushort SecretSoundID
        {
            get => Convert.ToUInt16(SecretSound);
            set => SecretSound = Convert.ToByte(value);
        }

        private List<string> _levelNames, _pictureNames, _titleFileNames, _rplFileNames, _levelFileNames, _cutSceneFileNames;
        internal IReadOnlyList<string> LevelNames => _levelNames;
        internal IReadOnlyList<string> PictureNames => _pictureNames;
        internal IReadOnlyList<string> TitleFileNames => _titleFileNames;
        internal IReadOnlyList<string> RPLFileNames => _rplFileNames;
        internal IReadOnlyList<string> LevelFileNames => _levelFileNames;
        internal IReadOnlyList<string> CutSceneFileNames => _cutSceneFileNames;

        private List<ushort[]> _scriptData;
        private List<ushort> _demoData;
        private List<uint[]> _psxFMVData;
        internal IReadOnlyList<ushort[]> ScriptData => _scriptData;
        internal IReadOnlyList<ushort> DemoData => _demoData;
        internal IReadOnlyList<uint[]> PSXFMVData => _psxFMVData;

        private List<string> _gameStrings1, _gameStrings2;
        private List<string> _puzzleNames1, _puzzleNames2, _puzzleNames3, _puzzleNames4;
        private List<string> _secretNames1, _secretNames2, _secretNames3, _secretNames4;
        private List<string> _specialNames1, _specialNames2;
        private List<string> _pickupNames1, _pickupNames2;
        private List<string> _keyNames1, _keyNames2, _keyNames3, _keyNames4;

        internal ushort NumGameStrings1 { get; private set; }
        internal ushort NumGameStrings2 { get; private set; }
        internal IReadOnlyList<string> GameStrings1 => _gameStrings1;
        internal IReadOnlyList<string> GameStrings2 => _gameStrings2;
                
        internal IReadOnlyList<string> PuzzleNames1 => _puzzleNames1;
        internal IReadOnlyList<string> PuzzleNames2 => _puzzleNames2;
        internal IReadOnlyList<string> PuzzleNames3 => _puzzleNames3;
        internal IReadOnlyList<string> PuzzleNames4 => _puzzleNames4;

        internal IReadOnlyList<string> SecretNames1 => _secretNames1;
        internal IReadOnlyList<string> SecretNames2 => _secretNames2;
        internal IReadOnlyList<string> SecretNames3 => _secretNames3;
        internal IReadOnlyList<string> SecretNames4 => _secretNames4;

        internal IReadOnlyList<string> SpecialNames1 => _specialNames1;
        internal IReadOnlyList<string> SpecialNames2 => _specialNames2;

        internal IReadOnlyList<string> PickupNames1 => _pickupNames1;
        internal IReadOnlyList<string> PickupNames2 => _pickupNames2;
                
        internal IReadOnlyList<string> KeyNames1 => _keyNames1;
        internal IReadOnlyList<string> KeyNames2 => _keyNames2;
        internal IReadOnlyList<string> KeyNames3 => _keyNames3;
        internal IReadOnlyList<string> KeyNames4 => _keyNames4;

        private byte[] _padding1;
        private byte[] _padding2;
        private byte[] _padding3;
        private byte[] _padding4;
        #endregion

        #region Flags
        internal enum Flag
        {
            DemoVersion = 0,
            TitleDisabled = 1,
            CheatsIgnored = 2,
            NoInputIgnored = 3,
            SaveLoadDisabled = 4,
            ScreensizingDisabled = 5,
            OptionRingDisabled = 6,
            DozyEnabled = 7, //only works on TR2 PSX final release
            UseScriptCypher = 8,
            GymEnabled = 9,
            LevelSelect = 10,
            EnableCheatCode = 11 //has no effect, see 2 instead
        }

        internal bool IsFlagSet(Flag flag)
        {
            int f = 1 << (int)flag;
            return (Flags & f) == f;
        }

        private void SetFlag(Flag flag, bool on)
        {
            if (on)
            {
                Flags |= Convert.ToUInt16(1 << (int)flag);
            }
            else
            {
                Flags = Convert.ToUInt16(Flags & ~(1 << (int)flag));
            }
        }

        internal bool CheatsIgnored
        {
            get => IsFlagSet(Flag.CheatsIgnored);
            set => SetFlag(Flag.CheatsIgnored, value);
        }

        internal bool DemosDisabled
        {
            get => IsFlagSet(Flag.NoInputIgnored);
            set => SetFlag(Flag.NoInputIgnored, value);
        }

        internal bool DemoVersion
        {
            get => IsFlagSet(Flag.DemoVersion);
            set => SetFlag(Flag.DemoVersion, value);
        }

        internal bool DozyEnabled
        {
            get => DozyViable && IsFlagSet(Flag.DozyEnabled);
            set => SetFlag(Flag.DozyEnabled, DozyViable && value);
        }

        internal bool DozyViable => Edition == TREdition.TR2PSXBeta;

        internal bool GymEnabled
        {
            get => IsFlagSet(Flag.GymEnabled);
            set => SetFlag(Flag.GymEnabled, value);
        }

        internal bool LevelSelectEnabled
        {
            get => IsFlagSet(Flag.LevelSelect);
            set => SetFlag(Flag.LevelSelect, value);
        }

        internal bool OptionRingDisabled
        {
            get => IsFlagSet(Flag.OptionRingDisabled);
            set => SetFlag(Flag.OptionRingDisabled, value);
        }

        internal bool SaveLoadDisabled
        {
            get => IsFlagSet(Flag.SaveLoadDisabled);
            set
            {
                SetFlag(Flag.SaveLoadDisabled, value);
                FirstOption = value ? 1u : 1280u;
            }
        }

        internal bool ScreensizingDisabled
        {
            get => IsFlagSet(Flag.ScreensizingDisabled);
            set => SetFlag(Flag.ScreensizingDisabled, value);
        }

        internal bool TitleDisabled
        {
            get => IsFlagSet(Flag.TitleDisabled);
            set
            {
                SetFlag(Flag.TitleDisabled, value);
                AdjustExitToTitleText();
            }
        }

        internal bool UseScriptCypher
        {
            get => IsFlagSet(Flag.UseScriptCypher);
            set => SetFlag(Flag.UseScriptCypher, value);
        }

        #endregion

        /**
         * If the title screen has been disabled, selecting "Exit to Title" from the passport during gameplay
         * will result in a new game starting. We store the "Exit to Title" text at the second to last position
         * in gamestrings2 (occupied as "spare" in all tr2/3 games) and replace "Exit to Title" with the existing 
         * value for "New Game". When re-enabling the title, Exit to Title is moved back to its original position,
         * and the final value in the array is reset.
         */
        private void AdjustExitToTitleText()
        {
            if (!TitleDisabled)
            {
                TitleReplace = -1;
                if (_gameStrings1[_gameStrings2.Count - 2].StartsWith("TRGE:"))
                {
                    _gameStrings1[8] = _gameStrings2[_gameStrings2.Count - 2].Substring(5); //exit to title
                    _gameStrings2[_gameStrings2.Count - 2] = _gameStrings2[_gameStrings2.Count - 3]; //spare
                }
            }
            else
            {
                TitleReplace = 1;
                _gameStrings2[_gameStrings2.Count - 2] = "TRGE:" + _gameStrings1[8]; //exit to title
                _gameStrings1[8] = _gameStrings1[6]; //new game
            }
        }

        #region TRLevel Interop

        private AbstractTRFrontEnd _frontEnd;
        internal override AbstractTRFrontEnd FrontEnd
        {
            get
            {
                if (_frontEnd == null)
                {
                    (_frontEnd = new TR23FrontEnd()).BuildOperations(_scriptData[0]);
                }
                return _frontEnd;
            }
            /*set
            {
                _scriptData[0] = value.TranslateOperations();
            }*/
        }

        internal override List<AbstractTRLevel> Levels
        {
            get
            {
                int count = NumLevels - NumDemoLevels;
                List<AbstractTRLevel> levels = new List<AbstractTRLevel>(count);
                for (ushort i = 1; i < count; i++) //skip assault and however many demos there are
                {
                    TR23Level level = new TR23Level
                    {
                        Name = _levelNames[i],
                        LevelFile = _levelFileNames[i]
                    };
                    levels.Add(level);

                    level.AddPuzzle(_puzzleNames1[i]);
                    level.AddPuzzle(_puzzleNames2[i]);
                    level.AddPuzzle(_puzzleNames3[i]);
                    level.AddPuzzle(_puzzleNames4[i]);

                    level.AddKey(_keyNames1[i]);
                    level.AddKey(_keyNames2[i]);
                    level.AddKey(_keyNames3[i]);
                    level.AddKey(_keyNames4[i]);

                    level.AddPickup(_pickupNames1[i]);
                    level.AddPickup(_pickupNames2[i]);

                    level.BuildOperations(_scriptData[i + 1]);
                }

                return levels;
            }
            set
            {
                if (value.Count != NumLevels - NumDemoLevels - 1)
                {
                    throw new ArgumentException("Invalid number of levels.");
                }

                for (int i = 0, j = 1; i < value.Count; i++, j++)
                {
                    _levelNames[j] = value[i].Name;
                    _levelFileNames[j] = value[i].LevelFile;

                    _puzzleNames1[j] = value[i].Puzzles[0];
                    _puzzleNames2[j] = value[i].Puzzles[1];
                    _puzzleNames3[j] = value[i].Puzzles[2];
                    _puzzleNames4[j] = value[i].Puzzles[3];

                    _keyNames1[j] = value[i].Keys[0];
                    _keyNames2[j] = value[i].Keys[1];
                    _keyNames3[j] = value[i].Keys[2];
                    _keyNames4[j] = value[i].Keys[3];

                    _pickupNames1[j] = value[i].Pickups[0];
                    _pickupNames2[j] = value[i].Pickups[1];

                    _scriptData[j + 1] = value[i].TranslateOperations();
                }
            }
        }
        #endregion

        #region IO
        protected override void CalculateEdition()
        {
            if (_levelFileNames == null)
            {
                return;
            }

            foreach (string levelFile in _levelFileNames)
            {
                string llf = levelFile.ToLower();
                if (llf.EndsWith("wall.tr2"))
                {
                    Edition = TREdition.TR2PC;
                }
                else if (llf.EndsWith("wall.psx"))
                {
                    Edition = Xor == 0 ? TREdition.TR2PSXBeta : TREdition.TR2PSX;
                }
                else if (llf.EndsWith("level1.tr2"))
                {
                    Edition = TREdition.TR2G;
                }
                else if (llf.EndsWith("jungle.tr2"))
                {
                    Edition = TREdition.TR3PC;
                }
                else if (llf.EndsWith("jungle.psx"))
                {
                    Edition = TREdition.TR3PSX;
                }
                else if (llf.EndsWith("scotland.tr2"))
                {
                    Edition = TREdition.TR3G;
                }
            }
        }

        internal override void Read(BinaryReader br)
        {
            if (br.ReadUInt32() != Version)
            {
                throw new ArgumentException("Unsupported script data file.");
            }

            _description = Encoding.ASCII.GetString(br.ReadBytes(256));
            int nt = _description.IndexOf('\0');
            if (nt != -1)
            {
                _description = _description.Substring(0, nt);
            }

            GameflowSize = br.ReadUInt16();
            FirstOption = br.ReadUInt32();
            TitleReplace = br.ReadInt32();
            DeathDemoMode = br.ReadUInt32();
            DeathInGame = br.ReadUInt32();
            DemoTime = br.ReadUInt32();
            DemoInterrupt = br.ReadUInt32();
            DemoEnd = br.ReadUInt32();
            _padding1 = br.ReadBytes(36);

            NumLevels = br.ReadUInt16();
            NumPictures = br.ReadUInt16();
            NumTitles = br.ReadUInt16();
            NumRPLs = br.ReadUInt16();
            NumCutScenes = br.ReadUInt16();
            NumDemoLevels = br.ReadUInt16();
            TitleSoundID = br.ReadUInt16();
            SingleLevel = br.ReadUInt16();
            _padding2 = br.ReadBytes(32);

            Flags = br.ReadUInt16();
            _padding3 = br.ReadBytes(6);

            Xor = br.ReadByte();
            Language = br.ReadByte();
            SecretSound = br.ReadByte();
            _padding4 = br.ReadBytes(5);

            _levelNames = ReadStringData(br, NumLevels);
            _pictureNames = ReadStringData(br, NumPictures);
            _titleFileNames = ReadStringData(br, NumTitles);
            _rplFileNames = ReadStringData(br, NumRPLs);
            _levelFileNames = ReadStringData(br, NumLevels);
            _cutSceneFileNames = ReadStringData(br, NumCutScenes);

            //we know the level file names at this point, so a decent estimate
            //can be made for the actual edition
            CalculateEdition();

            _scriptData = ReadScriptData(br);
            _demoData = ReadDemoData(br);
            _psxFMVData = ReadPSXFMVData(br);

            NumGameStrings1 = br.ReadUInt16();
            _gameStrings1 = ReadStringData(br, NumGameStrings1);            

            if (Edition == TREdition.TR2PSXBeta)
            {
                NumGameStrings2 = 79;
            }
            else if (Edition.Hardware == Hardware.PC)
            {
                NumGameStrings2 = 41;
            }
            else
            {
                NumGameStrings2 = 80;
            }
            _gameStrings2 = ReadStringData(br, NumGameStrings2);

            _puzzleNames1 = ReadStringData(br, NumLevels);
            _puzzleNames2 = ReadStringData(br, NumLevels);
            _puzzleNames3 = ReadStringData(br, NumLevels);
            _puzzleNames4 = ReadStringData(br, NumLevels);

            if (Edition == TREdition.TR2PSXBeta)
            {
                _secretNames1 = ReadStringData(br, NumLevels);
                _secretNames2 = ReadStringData(br, NumLevels);
                _secretNames3 = ReadStringData(br, NumLevels);
                _secretNames4 = ReadStringData(br, NumLevels);

                _specialNames1 = ReadStringData(br, NumLevels);
                _specialNames2 = ReadStringData(br, NumLevels);
            }

            _pickupNames1 = ReadStringData(br, NumLevels);
            _pickupNames2 = ReadStringData(br, NumLevels);

            _keyNames1 = ReadStringData(br, NumLevels);
            _keyNames2 = ReadStringData(br, NumLevels);
            _keyNames3 = ReadStringData(br, NumLevels);
            _keyNames4 = ReadStringData(br, NumLevels);
        }

        private List<string> ReadStringData(BinaryReader br, ushort size)
        {
            List<string> stringData = new List<string>(size);
            if (size == 0)
            {
                br.ReadUInt16(); //0
            }
            else
            {
                ushort[] offsets = new ushort[size + 1];
                for (int i = 0; i < offsets.Length; i++)
                {
                    offsets[i] = br.ReadUInt16();
                }


                for (int i = 0; i < size; i++)
                {

                    int target = offsets[i + 1] - offsets[i] - 1;
                    StringBuilder sb = new StringBuilder();
                    for (int k = 0; k < target; k++)
                    {
                        sb.Append(((char)((uint)br.ReadByte() ^ Xor)).ToString());
                    }
                    stringData.Add(sb.ToString());
                    br.ReadByte(); //xor
                }
            }

            return stringData;
        }

        private List<ushort[]> ReadScriptData(BinaryReader br)
        {
            ushort[] offsets = new ushort[NumLevels + 2];
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = (ushort)(br.ReadUInt16() / 2U);
            }

            List<ushort[]> scriptData = new List<ushort[]>(NumLevels + 1);
            for (int i = 0; i < NumLevels + 1; i++)
            {
                scriptData.Add(new ushort[offsets[i + 1] - offsets[i]]);
                for (int j = 0; j < scriptData[i].Length; j++)
                {
                    scriptData[i][j] = br.ReadUInt16();
                }
            }

            return scriptData;
        }

        private List<ushort> ReadDemoData(BinaryReader br)
        {
            List<ushort> demoData = new List<ushort>(NumDemoLevels);
            for (int i = 0; i < NumDemoLevels; i++)
            {
                demoData.Add(br.ReadUInt16());
            }

            return demoData;
        }

        private List<uint[]> ReadPSXFMVData(BinaryReader br)
        {
            List<uint[]> fmvData = new List<uint[]>(NumRPLs);
            if (Edition.Hardware == Hardware.PSX)
            {
                for (int i = 0; i < NumRPLs; i++)
                {
                    fmvData.Add(new uint[]
                    {
                        br.ReadUInt32(), br.ReadUInt32()
                    });
                }
            }
            return fmvData;
        }

        internal override byte[] Serialise()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(Version);

                byte[] description = Encoding.ASCII.GetBytes(_description);
                Array.Resize(ref description, 256);
                bw.Write(description);

                bw.Write(GameflowSize);
                bw.Write(FirstOption);
                bw.Write(TitleReplace);
                bw.Write(DeathDemoMode);
                bw.Write(DeathInGame);
                bw.Write(DemoTime);
                bw.Write(DemoInterrupt);
                bw.Write(DemoEnd);
                bw.Write(_padding1);

                bw.Write(NumLevels);
                bw.Write(NumPictures);
                bw.Write(NumTitles);
                bw.Write(NumRPLs);
                bw.Write(NumCutScenes);
                bw.Write(NumDemoLevels);
                bw.Write(TitleSoundID);
                bw.Write(SingleLevel);
                bw.Write(_padding2);

                bw.Write(Flags);
                bw.Write(_padding3);

                bw.Write(Xor);
                bw.Write(Language);
                bw.Write(SecretSound);
                bw.Write(_padding4);

                WriteStringData(bw, _levelNames);
                WriteStringData(bw, _pictureNames);
                WriteStringData(bw, _titleFileNames);
                WriteStringData(bw, _rplFileNames);
                WriteStringData(bw, _levelFileNames);
                WriteStringData(bw, _cutSceneFileNames);

                WriteScriptData(bw);
                WriteDemoData(bw);
                WritePSXFMVData(bw);

                bw.Write(NumGameStrings1);
                WriteStringData(bw, _gameStrings1);
                WriteStringData(bw, _gameStrings2);

                WriteStringData(bw, _puzzleNames1);
                WriteStringData(bw, _puzzleNames2);
                WriteStringData(bw, _puzzleNames3);
                WriteStringData(bw, _puzzleNames4);

                if (Edition == TREdition.TR2PSXBeta)
                {
                     WriteStringData(bw, _secretNames1);
                     WriteStringData(bw, _secretNames2);
                     WriteStringData(bw, _secretNames3);
                     WriteStringData(bw, _secretNames4);

                     WriteStringData(bw, _specialNames1);
                     WriteStringData(bw, _specialNames2);
                }

                WriteStringData(bw, _pickupNames1);
                WriteStringData(bw, _pickupNames2);

                WriteStringData(bw, _keyNames1);
                WriteStringData(bw, _keyNames2);
                WriteStringData(bw, _keyNames3);
                WriteStringData(bw, _keyNames4);

                return ms.ToArray();
            }
        }

        private void WriteStringData(BinaryWriter bw, List<string> stringData)
        {
            if (stringData.Count == 0)
            {
                bw.Write((ushort)0);
                return;
            }

            ushort runningOffset = 0;
            bw.Write(runningOffset);

            List<byte> encodedStringData = new List<byte>();
            for (int i = 0; i < stringData.Count; i++)
            {
                char[] chars = stringData[i].ToCharArray();
                runningOffset += Convert.ToUInt16(chars.Length + 1); //xor
                bw.Write(runningOffset);
                
                foreach (int j in chars)
                {
                    encodedStringData.Add((byte)int.Parse((j ^ Xor).ToString()));
                }
                encodedStringData.Add(Xor);
            }

            bw.Write(encodedStringData.ToArray());
        }

        private void WriteScriptData(BinaryWriter bw)
        {
            if (_scriptData.Count == 0)
            {
                return;
            }

            _scriptData[0] = FrontEnd.TranslateOperations();

            ushort runningOffset = 0;
            bw.Write(runningOffset);
            List<ushort> collectedScriptData = new List<ushort>();

            for (int i = 0; i < _scriptData.Count; i++)
            {
                ushort[] data = _scriptData[i];
                runningOffset += Convert.ToUInt16(data.Length);
                bw.Write(Convert.ToUInt16(runningOffset * 2U));

                for (int j = 0; j < data.Length; j++)
                {
                    collectedScriptData.Add(data[j]);
                }
            }

            foreach (ushort dat in collectedScriptData)
            {
                bw.Write(dat);
            }
        }

        private void WriteDemoData(BinaryWriter bw)
        {
            if (NumDemoLevels > 0)
            {
                foreach (ushort dd in DemoData)
                {
                    bw.Write(dd);
                }
            }
        }

        private void WritePSXFMVData(BinaryWriter bw)
        {
            if (Edition.Hardware == Hardware.PSX)
            {
                foreach (uint[] fmvData in _psxFMVData)
                {
                    foreach (uint dd in fmvData)
                    {
                        bw.Write(dd);
                    }
                }
            }
        }
        #endregion
    }
}