using Microsoft.UI.Xaml;
using System;

#if MICROSOFT_STORE
using Windows.Storage;
#endif

namespace DLSS_Swapper
{
    public class Settings
    {
        static Settings _instance = null;

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

        bool _hideNonDLSSGames = true;
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


        bool _groupGameLibrariesTogether = false;
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

        bool _allowExperimental = false;
        public bool AllowExperimental
        {
            get { return _allowExperimental; }
            set
            {
                if (_allowExperimental != value)
                {
                    _allowExperimental = value;
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
        LoggingLevel _loggingLevel = LoggingLevel.Error;
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


#if MICROSOFT_STORE
        static Settings FromLocalSettings()
        {
            var settings = new Settings();

            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.TryGetValue("HasShownWarning", out object tempHasShownWarning))
            {
                if (tempHasShownWarning is bool hasShownWarning)
                {
                    settings._hasShownWarning = hasShownWarning;
                }
            }


            if (localSettings.Values.TryGetValue("HasShownMultiplayerWarning", out object tempHasShownMultiplayerWarning))
            {
                if (tempHasShownMultiplayerWarning is bool hasShownMultiplayerWarning)
                {
                    settings._hasShownMultiplayerWarning = hasShownMultiplayerWarning;
                }
            }
                        

            if (localSettings.Values.TryGetValue("HideNonDLSSGames", out object tempHideNonDLSSGames))
            {
                if (tempHideNonDLSSGames is bool hideNonDLSSGames)
                {
                    settings._hideNonDLSSGames = hideNonDLSSGames;
                }
            }

            if (localSettings.Values.TryGetValue("GroupGameLibrariesTogether", out object tempGroupGameLibrariesTogether))
            {
                if (tempGroupGameLibrariesTogether is bool groupGameLibrariesTogether)
                {
                    settings._groupGameLibrariesTogether = groupGameLibrariesTogether;
                }
            }

            if (localSettings.Values.TryGetValue("AppTheme", out object tempAppTheme))
            {
                if (tempAppTheme is int appTheme)
                {
                    settings._appTheme = (ElementTheme)appTheme;
                }
            }

            if (localSettings.Values.TryGetValue("AllowExperimental", out object tempAllowExperimental))
            {
                if (tempAllowExperimental is bool allowExperimental)
                {
                    settings._allowExperimental = allowExperimental;
                }
            }

            if (localSettings.Values.TryGetValue("AllowUntrusted", out object tempAllowUntrusted))
            {
                if (tempAllowUntrusted is bool allowUntrusted)
                {
                    settings._allowUntrusted = allowUntrusted;
                }
            }

            if (localSettings.Values.TryGetValue("LastRecordsRefresh", out object tempLastRecordsRefresh))
            {
                if (tempLastRecordsRefresh is DateTimeOffset lastRecordsRefresh)
                {
                    settings._lastRecordsRefresh = lastRecordsRefresh;
                }
            }

            if (localSettings.Values.TryGetValue("LastPromptWasForVersion", out object tempLastPromptWasForVersion))
            {
                if (tempLastPromptWasForVersion is ulong lastPromptWasForVersion)
                {
                    settings._lastPromptWasForVersion = lastPromptWasForVersion;
                }
            }

            if (localSettings.Values.TryGetValue("LoggingLevel", out object tempLoggingLevel))
            {
                if (tempLoggingLevel is int loggingLevel)
                {
                    settings._loggingLevel = (LoggingLevel)loggingLevel;
                }
            }

            if (localSettings.Values.TryGetValue("EnabledGameLibraries", out object tempEnabledGameLibraries))
            {
                if (tempEnabledGameLibraries is uint enabledGameLibraries)
                {
                    settings._enabledGameLibraries = enabledGameLibraries;
                }
            }

            if (localSettings.Values.TryGetValue("WasLoadingGames", out object tempWasLoadingGames))
            {
                if (tempWasLoadingGames is bool wasLoadingGames)
                {
                    settings._wasLoadingGames = wasLoadingGames;
                }
            }

            // We only care about loading from here once. 
            // After we have got the values we will attempt to save as json.
            // It we fail to save as json these settings will be lost.
            localSettings.Values.Clear();

            return settings;
        }
#endif

        void SaveJson()
        {
            AsyncHelper.RunSync(() => Storage.SaveSettingsJsonAsync(this));
        }

        static Settings FromJson()
        {
            Settings settings = null;

            var settingsFromJson = AsyncHelper.RunSync(() => Storage.LoadSettingsJsonAsync());
            // If we couldn't load settings then save the defaults.
            if (settingsFromJson == null)
            {
#if MICROSOFT_STORE
                // If we are loading from an existing Microsoft Store build we want to copy over existing settings and then save as a json.
                settings = FromLocalSettings();
#else
                settings = new Settings();
#endif
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
