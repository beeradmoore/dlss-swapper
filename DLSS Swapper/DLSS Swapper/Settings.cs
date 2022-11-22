using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace DLSS_Swapper
{
    public class Settings
    {
        static Settings _instance = null;

#if WINDOWS_STORE
        public static Settings Instance => _instance ??= Settings.FromLocalSettings();
#else
        public static Settings Instance => _instance ??= Settings.FromJson();
#endif

        bool _hasShownWarning = false;
        public bool HasShownWarning
        {
            get { return _hasShownWarning; }
            set
            {
                if (_hasShownWarning != value)
                {
                    _hasShownWarning = value;

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["HasShownWarning"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["HasShownMultiplayerWarning"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["HideNonDLSSGames"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["GroupGameLibrariesTogether"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["AppTheme"] = (int)value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["AllowExperimental"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["AllowUntrusted"] = value;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["LastRecordsRefresh"] = Windows.Foundation.PropertyValue.CreateDateTime(value);
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["LastPromptWasForVersion"] = _lastPromptWasForVersion;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["LoggingLevel"] = (int)_loggingLevel;
#else
                    SaveJson();
#endif
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

                    // If this is the inital load we don't trigger any saves.
                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["EnabledGameLibraries"] = (uint)_enabledGameLibraries;
#else
                    SaveJson();
#endif
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

                    if (_isLoading)
                    {
                        return;
                    }
#if WINDOWS_STORE
                    ApplicationData.Current.LocalSettings.Values["WasLoadingGames"] = value;
#else
                    SaveJson();
#endif
                }
            }
        }

        // Used to prevent saving data while we are loading.
        bool _isLoading = true;


#if WINDOWS_STORE
        private static Settings FromLocalSettings()
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
            
            // Toggle _isLoading back to false.
            settings._isLoading = false;
            return settings;
        }
#else
        void SaveJson()
        {
            var settingsJson = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("settings.json", settingsJson);
        }

        private static Settings FromJson()
        {
            var settings = new Settings();
            try
            {
                if (File.Exists("settings.json") == false)
                {
                    settings.SaveJson();
                }
                else
                {
                    var settingsJsonText = File.ReadAllText("settings.json");
                    var settingsJsonObject = JsonSerializer.Deserialize<Settings>(settingsJsonText);
                    if (settingsJsonObject != null)
                    {
                        settings = settingsJsonObject;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);

                // We failed to load (or save initial) settings.json, so lets delete it and try agian next launch.
                try
                {
                    File.Delete("settings.json");
                }
                catch (Exception err2)
                {
                    // Something really bad happened and we couldn't delete it either.
                    Logger.Error(err2.Message);
                }
            }

            // Toggle _isLoading back to false.
            settings._isLoading = false;
            return settings;
        }
#endif
    }
}
