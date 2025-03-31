using DLSS_Swapper.Data;
using DLSS_Swapper.Pages;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DLSS_Swapper
{
    public class Settings
    {
        static Settings? _instance = null;

        public static Settings Instance => _instance ??= Settings.FromJson();

        // We default this to false to prevent saves firing when loading from json.
        bool _autoSave = false;

        bool _hasShownWarning = false;
        public bool HasShownWarning
        {
            get { return _hasShownWarning; }
            set
            {
                if (_hasShownWarning != value)
                {
                    _hasShownWarning = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        bool _hasShownMultiplayerWarning = false;
        public bool HasShownMultiplayerWarning
        {
            get { return _hasShownMultiplayerWarning; }
            set
            {
                if (_hasShownMultiplayerWarning != value)
                {
                    _hasShownMultiplayerWarning = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        bool _hideNonDLSSGames = false;
        public bool HideNonDLSSGames
        {
            get { return _hideNonDLSSGames; }
            set
            {
                if (_hideNonDLSSGames != value)
                {
                    _hideNonDLSSGames = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        bool _groupGameLibrariesTogether = true;
        public bool GroupGameLibrariesTogether
        {
            get { return _groupGameLibrariesTogether; }
            set
            {
                if (_groupGameLibrariesTogether != value)
                {
                    _groupGameLibrariesTogether = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        ElementTheme _appTheme = ElementTheme.Default;
        public ElementTheme AppTheme
        {
            get { return _appTheme; }
            set
            {
                if (_appTheme != value)
                {
                    _appTheme = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        bool _allowDebugDlls = false;
        public bool AllowDebugDlls
        {
            get { return _allowDebugDlls; }
            set
            {
                if (_allowDebugDlls != value)
                {
                    _allowDebugDlls = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }



        bool _allowUntrusted = false;
        public bool AllowUntrusted
        {
            get { return _allowUntrusted; }
            set
            {
                if (_allowUntrusted != value)
                {
                    _allowUntrusted = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        DateTimeOffset _lastRecordsRefresh = DateTimeOffset.MinValue;
        public DateTimeOffset LastRecordsRefresh
        {
            get { return _lastRecordsRefresh; }
            set
            {
                if (_lastRecordsRefresh != value)
                {
                    _lastRecordsRefresh = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        ulong _lastPromptWasForVersion = 0L;
        public ulong LastPromptWasForVersion
        {
            get { return _lastPromptWasForVersion; }
            set
            {
                if (_lastPromptWasForVersion != value)
                {
                    _lastPromptWasForVersion = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        // Don't forget to change this back to off.
#if DEBUG
        LoggingLevel _loggingLevel = LoggingLevel.Verbose;
#else
        LoggingLevel _loggingLevel = LoggingLevel.Error;
#endif
        public LoggingLevel LoggingLevel
        {
            get { return _loggingLevel; }
            set
            {
                if (_loggingLevel != value)
                {
                    _loggingLevel = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        uint _enabledGameLibraries = uint.MaxValue;
        public uint EnabledGameLibraries
        {
            get { return _enabledGameLibraries; }
            set
            {
                if (_enabledGameLibraries != value)
                {
                    _enabledGameLibraries = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        bool _wasLoadingGames = false;
        public bool WasLoadingGames
        {
            get { return _wasLoadingGames; }
            set
            {
                if (_wasLoadingGames != value)
                {
                    _wasLoadingGames = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        bool _dontShowManuallyAddingGamesNotice = false;
        public bool DontShowManuallyAddingGamesNotice
        {
            get { return _dontShowManuallyAddingGamesNotice; }
            set
            {
                if (_dontShowManuallyAddingGamesNotice != value)
                {
                    _dontShowManuallyAddingGamesNotice = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        bool _hasShownAddGameFolderMessage = false;
        public bool HasShownAddGameFolderMessage
        {
            get { return _hasShownAddGameFolderMessage; }
            set
            {
                if (_hasShownAddGameFolderMessage != value)
                {
                    _hasShownAddGameFolderMessage = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        WindowPositionRect _lastWindowSizeAndPosition = new WindowPositionRect();
        public WindowPositionRect LastWindowSizeAndPosition
        {
            get { return _lastWindowSizeAndPosition; }
            set
            {
                if (_lastWindowSizeAndPosition != value)
                {
                    _lastWindowSizeAndPosition = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        GameGridViewType _gameGridViewType = GameGridViewType.GridView;
        public GameGridViewType GameGridViewType
        {
            get { return _gameGridViewType; }
            set
            {
                if (_gameGridViewType != value)
                {
                    _gameGridViewType = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }


        int _gridViewItemWidth = 200;
        public int GridViewItemWidth
        {
            get { return _gridViewItemWidth; }
            set
            {
                if (_gridViewItemWidth != value)
                {
                    _gridViewItemWidth = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        bool _onlyShowDownloadedDlls = false;
        public bool OnlyShowDownloadedDlls
        {
            get { return _onlyShowDownloadedDlls; }
            set
            {
                if (_onlyShowDownloadedDlls != value)
                {
                    _onlyShowDownloadedDlls = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        IEnumerable<LogicalDriveState> _logicalDriveStates = DriveInfo.GetDrives().Select(d => new LogicalDriveState { DriveLetter = d.Name, IsEnabled = true });
        public IEnumerable<LogicalDriveState> LogicalDriveStates
        {
            get { return _logicalDriveStates; }
            set
            {
                if (_logicalDriveStates != value)
                {
                    _logicalDriveStates = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        /*
        public List<string> Directories { get; set; } = new List<string>();

        public void AddDirectory(string directory)
        {
            if (Directories.Contains(directory))
            {
                return;
            }
            
            Directories.Add(directory);
            
            if (_autoSave)
            {
                SaveJson();
            }
        }
        
        public void RemoveDirectory(string directory)
        {
            Directories.Remove(directory);
            
            if (_autoSave)
            {
                SaveJson();
            }
        }
        */

        void SaveJson()
        {
            AsyncHelper.RunSync(() => Storage.SaveSettingsJsonAsync(this));
        }

        static Settings FromJson()
        {
            Settings? settings = null;

            var settingsFromJson = AsyncHelper.RunSync(() => Storage.LoadSettingsJsonAsync());
            // If we couldn't load settings then save the defaults.
            if (settingsFromJson is null)
            {
                settings = new Settings();
                settings.SaveJson();
            }
            else
            {
                settings = settingsFromJson;
            }

            // Re-enable auto save.
            settings._autoSave = true;
            return settings;
        }
    }
}
