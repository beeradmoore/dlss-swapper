using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Management;
using System.Security.Principal;
using Windows.UI.ViewManagement;

namespace DLSS_Swapper
{
    // Class inspired by https://stackoverflow.com/a/69604613/1253832

    public class ThemeWatcher
    {
        private const string RegistryThemeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryThemeValueName = "AppsUseLightTheme";
        private const string RegistryContrastKeyPath = @"Control Panel\Accessibility\HighContrast";
        private const string RegistryContrastValueName = "LastUpdatedThemeId";

        ManagementEventWatcher? _themeWatcher;
        ManagementEventWatcher? _contrastWatcher;
        AccessibilitySettings _accessibilitySettings;
        ApplicationTheme _defaultApplicationTheme;

        public enum WindowsTheme
        {
            Default = 0,
            Light = 1,
            Dark = 2,
            HighContrast = 3
        }

        public event EventHandler<ApplicationTheme>? ThemeChanged = null;
        public event EventHandler<bool>? ContrastChanged = null;



        public bool IsWatchingTheme { get; private set; } = false;
        public bool IsWatchingContrast { get; private set; } = false;

        public bool HighContrast { get { return _accessibilitySettings.HighContrast; } }


        public ThemeWatcher(ApplicationTheme defaultApplicationTheme = ApplicationTheme.Dark)
        {
            _accessibilitySettings = new AccessibilitySettings();
            _defaultApplicationTheme = defaultApplicationTheme;
        }

        public void Start()
        {
            // Cleanup in case start is called twice.
            Stop();


            var currentUser = WindowsIdentity.GetCurrent();

            var themeQuery = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User?.Value ?? string.Empty,
                RegistryThemeKeyPath.Replace(@"\", @"\\"),
                RegistryThemeValueName);

            var contrastQuery = string.Format(
               CultureInfo.InvariantCulture,
               @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
               currentUser.User?.Value ?? string.Empty,
               RegistryContrastKeyPath.Replace(@"\", @"\\"),
               RegistryContrastValueName);


            try
            {
                _themeWatcher = new ManagementEventWatcher(themeQuery);
                _themeWatcher.EventArrived += ThemeWatcher_EventArrived;
                _themeWatcher.Start();
                IsWatchingTheme = true;
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            try
            {
                _contrastWatcher = new ManagementEventWatcher(contrastQuery);
                _contrastWatcher.EventArrived += ContrastWatcher_EventArrived;
                _contrastWatcher.Start();
                IsWatchingContrast = true;
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            Logger.Info($"{GetWindowsTheme()}, {HighContrast}");

        }

        public void Stop()
        {
            if (_themeWatcher is not null)
            {
                _themeWatcher.EventArrived -= ContrastWatcher_EventArrived;
                _themeWatcher.Stop();
                _themeWatcher = null;
                IsWatchingTheme = false;
            }

            if (_contrastWatcher is not null)
            {
                _contrastWatcher.EventArrived -= ContrastWatcher_EventArrived;
                _contrastWatcher.Stop();
                _contrastWatcher = null;
                IsWatchingContrast = false;
            }
        }

        private void ContrastWatcher_EventArrived(object? sender, EventArrivedEventArgs e)
        {
            Logger.Info($"{GetWindowsTheme()}, {HighContrast}");

            ContrastChanged?.Invoke(this, HighContrast);
        }

        private void ThemeWatcher_EventArrived(object? sender, EventArrivedEventArgs e)
        {
            Logger.Info($"{GetWindowsTheme()}, {HighContrast}");

            ThemeChanged?.Invoke(this, GetWindowsApplicationTheme());
        }

        public ApplicationTheme GetWindowsApplicationTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryThemeKeyPath))
                {
                    if (key?.GetValue(RegistryThemeValueName) is int registryValue)
                    {
                        return registryValue > 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            return _defaultApplicationTheme;
        }


        public WindowsTheme GetWindowsTheme()
        {
            WindowsTheme theme = WindowsTheme.Light;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryThemeKeyPath))
                {
                    if (key?.GetValue(RegistryThemeValueName) is int registryValue)
                    {
                        theme = registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;

                        return theme;
                    }
                }

                return theme;
            }
            catch (Exception err)
            {
                Logger.Error(err);
                return theme;
            }
        }
    }
}
