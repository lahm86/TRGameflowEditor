﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TRGE.Coord;
using TRGE.Core;
using TRGE.View.Model;
using TRGE.View.Utils;
using TRGE.View.Windows;

namespace TRGE.View.Controls
{
    /// <summary>
    /// Interaction logic for EditorControl.xaml
    /// </summary>
    public partial class EditorControl : UserControl
    {
        #region Dependency Properties
        public static readonly DependencyProperty TREditionProperty = DependencyProperty.Register
        (
            "Edition", typeof(string), typeof(EditorControl)
        );

        public static readonly DependencyProperty DataFolderProperty = DependencyProperty.Register
        (
            "DataFolder", typeof(string), typeof(EditorControl), new PropertyMetadata(string.Empty)
        );

        public string Edition
        {
            get => (string)GetValue(TREditionProperty);
            private set => SetValue(TREditionProperty, value);
        }

        public string DataFolder
        {
            get => (string)GetValue(DataFolderProperty);
            private set => SetValue(DataFolderProperty, value);
        }
        #endregion

        private readonly EditorOptions _options;
        private bool _dirty;

        public event EventHandler<EditorEventArgs> EditorStateChanged;

        public TREditor Editor { get; private set; }

        public EditorControl()
        {
            InitializeComponent();
            _editorGrid.DataContext = _options = new EditorOptions();
            _options.PropertyChanged += Editor_PropertyChanged;
            _dirty = false;
        }

        private void Editor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _dirty = true;
            FireEditorStateChanged();
        }

        private void FireEditorStateChanged()
        {
            EditorStateChanged?.Invoke(this, new EditorEventArgs
            { 
                IsDirty = _dirty,
                CanExport = Editor != null && Editor.IsExportPossible
            });
        }

        public void Load(DataFolderEventArgs e)
        {
            Editor = e.Editor;
            Edition = Editor.Edition.Title;
            DataFolder = e.DataFolder;
            Reload();
        }

        private void Reload()
        {
            _options.Load(Editor.ScriptEditor as TR23ScriptEditor);
            _dirty = false;
            FireEditorStateChanged();
        }

        public bool Save()
        {
            SaveProgressWindow spw = new SaveProgressWindow(Editor, _options);
            if (spw.ShowDialog() ?? false)
            {
                _dirty = false;
                FireEditorStateChanged();
                return true;
            }
            return false;
        }

        public void Unload()
        {
            Editor = null;
            _dirty = false;
            FireEditorStateChanged();
        }

        public void OpenBackupFolder()
        {
            Process.Start("explorer.exe", Editor.BackupDirectory);
        }

        public void RestoreDefaults()
        {
            if (MessageWindow.ShowConfirm("The files that were backed up when this folder was first opened will be copied back to the original directory.\n\nDo you wish to proceed?"))
            {
                Editor.Restore();
                Reload();
                MessageWindow.ShowMessage("The restore completed successfully.");
            }
        }

        public void ImportSettings()
        {
            using (CommonOpenFileDialog dlg = new CommonOpenFileDialog())
            {
                dlg.Filters.Add(new CommonFileDialogFilter("TRGE Files", "trge"));
                dlg.Title = "TRGE : Import Settings";
                if (dlg.ShowDialog(WindowUtils.GetActiveWindowHandle()) == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        Editor.ImportSettings(dlg.FileName);
                        _options.Load(Editor.ScriptEditor as TR23ScriptEditor);
                    }
                    catch (Exception e)
                    {
                        MessageWindow.ShowError(e.Message);
                    }
                }
            }
        }

        public void ExportSettings()
        {
            if (!ConfirmExport())
            {
                return;
            }

            using (CommonSaveFileDialog dlg = new CommonSaveFileDialog())
            {
                dlg.DefaultFileName = Edition.ToSafeFileName() + ".trge";
                dlg.DefaultExtension = ".trge";
                dlg.Filters.Add(new CommonFileDialogFilter("TRGE Files", "trge"));
                dlg.OverwritePrompt = true;
                dlg.Title = "TRGE : Export Settings";
                if (dlg.ShowDialog(WindowUtils.GetActiveWindowHandle()) == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        Editor.ExportSettings(dlg.FileName);
                    }
                    catch (Exception e)
                    {
                        MessageWindow.ShowError(e.Message);
                    }
                }
            }
        }

        private bool ConfirmExport()
        {
            int unviableCount = _options.GetUnviableCount();
            if (unviableCount > 0)
            {
                StringBuilder sb = new StringBuilder("As the following items have been edited externally, they will be reset to default in the exported file.");
                sb.Append(Environment.NewLine);
                if (!_options.LevelSequencingViable)
                {
                    sb.Append(Environment.NewLine).Append("Level Sequencing");
                }
                if (!_options.UnarmedLevelsViable)
                {
                    sb.Append(Environment.NewLine).Append("Unarmed Levels");
                }
                if (!_options.AmmolessLevelsViable)
                {
                    sb.Append(Environment.NewLine).Append("Ammoless Levels");
                }
                if (_options.SecretRewardsSupported && !_options.SecretRewardsViable)
                {
                    sb.Append(Environment.NewLine).Append("Secret Rewards");
                }
                if (_options.SunsetsSupported && !_options.SunsetsViable)
                {
                    sb.Append(Environment.NewLine).Append("Sunsets");
                }
                if (!_options.AudioViable)
                {
                    sb.Append(Environment.NewLine).Append("Audio");
                }

                return MessageWindow.ShowWarningWithCancel(sb.ToString());
            }

            return true;
        }

        private void LevelSequencing_ManualConfigure(object sender, RoutedEventArgs e)
        {
            LevelSequenceWindow lsw = new LevelSequenceWindow(_options.LevelSequencing);
            if (lsw.ShowDialog() ?? false)
            {
                _options.LevelSequencing = lsw.LevelSequencingData;
            }
        }

        private void UnarmedLevels_ManualConfigure(object sender, RoutedEventArgs e)
        {
            UnarmedLevelsWindow ulw = new UnarmedLevelsWindow(_options.UnarmedLevelData);
            if (ulw.ShowDialog() ?? false)
            {
                _options.UnarmedLevelData = ulw.LevelData;
            }
        }

        private void AmmolessLevels_ManualConfigure(object sender, RoutedEventArgs e)
        {
            AmmolessLevelsWindow alw = new AmmolessLevelsWindow(_options.AmmolessLevelData);
            if (alw.ShowDialog() ?? false)
            {
                _options.AmmolessLevelData = alw.LevelData;
            }
        }

        private void SecretRewards_ManualConfigure(object sender, RoutedEventArgs e)
        {
            BonusItemsWindow biw = new BonusItemsWindow(_options.SecretBonusData);
            if (biw.ShowDialog() ?? false)
            {
                _options.SecretBonusData = biw.SecretBonusData;
            }
        }

        private void Sunsets_ManualConfigure(object sender, RoutedEventArgs e)
        {
            SunsetLevelsWindow slw = new SunsetLevelsWindow(_options.SunsetLevelData);
            if (slw.ShowDialog() ?? false)
            {
                _options.SunsetLevelData = slw.LevelData;
            }
        }

        private void Audio_ManualConfigure(object sender, RoutedEventArgs e)
        {
            AudioWindow aw = new AudioWindow(_options);
            if (aw.ShowDialog() ?? false)
            {
                _options.GlobalAudioData = aw.AudioData;
            }
        }

        private void LevelSequencing_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external level sequencing changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetLevelSequencingViable();
            }
        }

        private void UnarmedLevels_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external unarmed level changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetUnarmedLevelsViable();
            }
        }

        private void AmmolessLevels_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external ammoless level changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetAmmolessLevelsViable();
            }
        }

        private void SecretRewards_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external secret reward changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetSecretRewardsViable();
            }
        }

        private void Sunsets_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external sunsets changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetSunsetsViable();
            }
        }

        private void Audio_ChangeViability(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowConfirm("Any external audio changes made to the current configuration will be lost during the next save.\n\nDo you wish to proceed?"))
            {
                _options.SetAudioViable();
            }
        }
    }
}