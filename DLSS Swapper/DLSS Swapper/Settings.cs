using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DLSS_Swapper
{
    public static class Settings
    {
        static bool _hasShownWarning = false;
        public static bool HasShownWarning
        {
            get { return _hasShownWarning; }
            set
            {
                if (_hasShownWarning != value)
                {
                    _hasShownWarning = value;
                    ApplicationData.Current.LocalSettings.Values["HasShownWarning"] = value;
                }
            }
        }


        static bool _hasShownWorkInProgress = false;
        public static bool HasShownWorkInProgress
        {
            get { return _hasShownWorkInProgress; }
            set
            {
                if (_hasShownWorkInProgress != value)
                {
                    _hasShownWorkInProgress = value;
                    ApplicationData.Current.LocalSettings.Values["HasShownWorkInProgress"] = value;
                }
            }
        }


        static bool _hideNonDLSSGames = true;
        public static bool HideNonDLSSGames
        {
            get { return _hideNonDLSSGames; }
            set
            {
                if (_hideNonDLSSGames != value)
                {
                    _hideNonDLSSGames = value;
                    ApplicationData.Current.LocalSettings.Values["HideNonDLSSGames"] = value;
                }
            }
        }


        static bool _groupGameLibrariesTogether = false;
        public static bool GroupGameLibrariesTogether
        {
            get { return _groupGameLibrariesTogether; }
            set
            {
                if (_groupGameLibrariesTogether != value)
                {
                    _groupGameLibrariesTogether = value;
                    ApplicationData.Current.LocalSettings.Values["GroupGameLibrariesTogether"] = value;
                }
            }
        }

        static ElementTheme _appTheme = ElementTheme.Default;
        public static ElementTheme AppTheme
        {
            get { return _appTheme; }
            set
            {
                if (_appTheme != value)
                {
                    _appTheme = value;
                    ApplicationData.Current.LocalSettings.Values["AppTheme"] = (int)value;
                }
            }
        }

        static bool _allowExperimental = false;
        public static bool AllowExperimental
        {
            get { return _allowExperimental; }
            set
            {
                if (_allowExperimental != value)
                {
                    _allowExperimental = value;
                    ApplicationData.Current.LocalSettings.Values["AllowExperimental"] = value;
                }
            }
        }


        static bool _allowUntrusted = false;
        public static bool AllowUntrusted
        {
            get { return _allowUntrusted; }
            set
            {
                if (_allowUntrusted != value)
                {
                    _allowUntrusted = value;
                    ApplicationData.Current.LocalSettings.Values["AllowUntrusted"] = value;
                }
            }
        }

        static DateTimeOffset _lastRecordsRefresh = DateTimeOffset.MinValue;
        public static DateTimeOffset LastRecordsRefresh
        {
            get { return _lastRecordsRefresh; }
            set
            {
                if (_lastRecordsRefresh != value)
                {
                    _lastRecordsRefresh = value;
                    ApplicationData.Current.LocalSettings.Values["LastRecordsRefresh"] = Windows.Foundation.PropertyValue.CreateDateTime(value);
                }
            }
        }

        static bool _migrationAttempted = false;
        public static bool MigrationAttempted
        {
            get { return _migrationAttempted; }
            set
            {
                if (_migrationAttempted != value)
                {
                    _migrationAttempted = value;
                    ApplicationData.Current.LocalSettings.Values["MigrationAttempted"] = value;
                }
            }
        }

        static Settings()
        {
            // Load BaseDirectory from settings.
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.TryGetValue("HasShownWarning", out object tempHasShownWarning))
            {
                if (tempHasShownWarning is bool hasShownWarning)
                {
                    _hasShownWarning = hasShownWarning;
                }
            }

            if (localSettings.Values.TryGetValue("HasShownWorkInProgress", out object tempHasShownWorkInProgress))
            {
                if (tempHasShownWorkInProgress is bool hasShownWorkInProgress)
                {
                    _hasShownWorkInProgress = hasShownWorkInProgress;
                }
            }

            if (localSettings.Values.TryGetValue("HideNonDLSSGames", out object tempHideNonDLSSGames))
            {
                if (tempHideNonDLSSGames is bool hideNonDLSSGames)
                {
                    _hideNonDLSSGames = hideNonDLSSGames;
                }
            }

            if (localSettings.Values.TryGetValue("GroupGameLibrariesTogether", out object tempGroupGameLibrariesTogether))
            {
                if (tempGroupGameLibrariesTogether is bool groupGameLibrariesTogether)
                {
                    _groupGameLibrariesTogether = groupGameLibrariesTogether;
                }
            }

            if (localSettings.Values.TryGetValue("AppTheme", out object tempAppTheme))
            {
                if (tempAppTheme is int appTheme)
                {
                    _appTheme = (ElementTheme)appTheme;
                }
            }

            if (localSettings.Values.TryGetValue("AllowExperimental", out object tempAllowExperimental))
            {
                if (tempAllowExperimental is bool allowExperimental)
                {
                    _allowExperimental = allowExperimental;
                }
            }

            if (localSettings.Values.TryGetValue("AllowUntrusted", out object tempAllowUntrusted))
            {
                if (tempAllowUntrusted is bool allowUntrusted)
                {
                    _allowUntrusted = allowUntrusted;
                }
            }

            if (localSettings.Values.TryGetValue("LastRecordsRefresh", out object tempLastRecordsRefresh))
            {
                if (tempLastRecordsRefresh is DateTimeOffset lastRecordsRefresh)
                {
                    _lastRecordsRefresh = lastRecordsRefresh;
                }
            }

            if (localSettings.Values.TryGetValue("MigrationAttempted", out object tempMigrationAttempted))
            {
                if (tempMigrationAttempted is bool migrationAttempted)
                {
                    _migrationAttempted = migrationAttempted;
                }
            }
        }
    }
}
