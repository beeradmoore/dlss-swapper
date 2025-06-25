using CommunityToolkit.Mvvm.Messaging;
using DLSS_Swapper.Data;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.Pages;
using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;

namespace DLSS_Swapper
{
    public class Settings
    {
        static Settings? _instance = null;

        public static Settings Instance => _instance ??= Settings.FromJson();
        //public event EnabledGameLibrariesChangedHandler EnabledGameLibrariesChanged;
        //public delegate Task EnabledGameLibrariesChangedHandler(object sender, EventArgs e);

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
        [Obsolete("This property is deprecated. Use GameLibrarySettings array instead.")]
        public uint EnabledGameLibraries
        {
            get { return _enabledGameLibraries; }
            set
            {
                if (_enabledGameLibraries != value)
                {
                    _enabledGameLibraries = value;
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

        string _language = string.Empty;
        public string Language
        {
            get { return _language; }
            set
            {
                if (_language != value)
                {
                    _language = value;
                    if (_autoSave)
                    {
                        SaveJson();
                    }
                }
            }
        }

        string[] _ignoredPaths = new string[0];
        public string[] IgnoredPaths
        {
            get { return _ignoredPaths; }
            set
            {
                if (_ignoredPaths != value)
                {
                    _ignoredPaths = value;
                    if (_autoSave)
                    {
                        SaveJson();
                        WeakReferenceMessenger.Default.Send(new Messages.GameLibrariesStateChangedMessage());
                    }
                }
            }
        }

        GameLibrarySettings[] _gameLibrarySettings = Array.Empty<GameLibrarySettings>();
        public GameLibrarySettings[] GameLibrarySettings
        {
            get { return _gameLibrarySettings; }
            set
            {
                if (_gameLibrarySettings != value)
                {
                    _gameLibrarySettings = value;
                    if (_autoSave)
                    {
                        SaveJson();
                        WeakReferenceMessenger.Default.Send(new Messages.GameLibrariesOrderChangedMessage());
                    }
                }
            }
        }

        internal void SaveJson()
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

            var shouldSave = settings.CheckGameLibraries();
            if (shouldSave)
            {
                settings.SaveJson();
            }

            // Re-enable auto save.
            settings._autoSave = true;
            return settings;
        }

        /// <summary>
        /// Checks game libraries to see if there are any new ones to be added, or misconfigured settings.
        /// </summary>
        /// <returns></returns>
        private bool CheckGameLibraries()
        {
            var gameLibraries = Enum.GetValues<GameLibrary>().ToList();

            // If no items are in the list we are migrating them all
            // If there are some items in the list we are only checking and adding new libraries
            if (_gameLibrarySettings.Length == 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var enabledGameLibraries = (GameLibrary)EnabledGameLibraries;
#pragma warning restore CS0618 // Type or member is obsolete

                var tempGameLibraries = new List<GameLibrarySettings>(gameLibraries.Count);
                foreach (var gameLibrary in gameLibraries)
                {
                    // Enaable libraries based on EnabledGameLibraries property.
                    tempGameLibraries.Add(new GameLibrarySettings()
                    {
                        GameLibrary = gameLibrary,
                        IsEnabled = enabledGameLibraries.HasFlag(gameLibrary),
                    });
                }
                _gameLibrarySettings = tempGameLibraries.ToArray();
                return true;
            }
            else
            {
                // Remove each one of the loaded gameLibraries from the list.
                foreach (var gameLibrarySetting in _gameLibrarySettings)
                {
                    gameLibraries.Remove(gameLibrarySetting.GameLibrary);
                }

                // If there are any items it could be a new launch, new library, or misconfigured settings.
                if (gameLibraries.Count > 0)
                {
                    var tempGameLibraries = new List<GameLibrarySettings>(_gameLibrarySettings);
                    foreach (var gameLibrary in gameLibraries)
                    {
                        // Because this is not a full migration new libraries are enabled by default.
                        tempGameLibraries.Add(new GameLibrarySettings()
                        {
                            GameLibrary = gameLibrary,
                            IsEnabled = true,
                        });
                    }
                    _gameLibrarySettings = tempGameLibraries.ToArray();
                    return true;
                }
            }

            return false;
        }
    }
}
