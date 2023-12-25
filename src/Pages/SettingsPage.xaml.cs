using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers.Commands;
using MvvmHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.System;
using Windows.UI.ViewManagement;
using System.Diagnostics;
using DLSS_Swapper.UserControls;

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// Page for application settings. A lot of this was taken from Xaml-Controls-Gallery, https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/SettingsPage.xaml
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        //https://github.com/microsoft/Xaml-Controls-Gallery/blob/6450265cc94da5b2fac5e1e22d1be35dc66c402e/XamlControlsGallery/Navigation/NavigationRootPage.xaml.cs#L32


        public string Version => App.CurrentApp.GetVersionString();

        private AsyncCommand _checkForUpdateCommand;
        public AsyncCommand CheckForUpdatesCommand => _checkForUpdateCommand ??= new AsyncCommand(CheckForUpdatesAsync, _=> !IsCheckingForUpdates);

        bool _isCheckingForUpdates;
        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            set
            {
                if (_isCheckingForUpdates != value)
                {
                    _isCheckingForUpdates = value;
                    CheckForUpdatesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public IEnumerable<LoggingLevel> LoggingLevels = Enum.GetValues<LoggingLevel>();

        public string CurrentLogPath => Logger.GetCurrentLogPath();

        public SettingsPage()
        {
            this.InitializeComponent();


            // Initilize defaults.
            LightThemeRadioButton.IsChecked = Settings.Instance.AppTheme == ElementTheme.Light;
            DarkThemeRadioButton.IsChecked = Settings.Instance.AppTheme == ElementTheme.Dark;
            DefaultThemeRadioButton.IsChecked = Settings.Instance.AppTheme == ElementTheme.Default;

            AllowUntrustedToggleSwitch.IsOn = Settings.Instance.AllowUntrusted;
            AllowExperimentalToggleSwitch.IsOn = Settings.Instance.AllowExperimental;
            LoggingComboBox.SelectedItem = Settings.Instance.LoggingLevel;

            DataContext = this;
        }

        void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }

            if (e.OriginalSource is RadioButton radioButton)
            {
                if (radioButton.Tag is string radioButtonTag)
                {
                    var newTheme = radioButtonTag switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default,
                    };

                    Settings.Instance.AppTheme = newTheme;
                    ((App)Application.Current)?.MainWindow?.UpdateColors(newTheme);
                }
            }
        }

        void AllowExperimental_Toggled(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }

            if (e.OriginalSource is ToggleSwitch toggleSwitch)
            {
                Settings.Instance.AllowExperimental = toggleSwitch.IsOn;
                App.CurrentApp.MainWindow.FilterDLSSRecords();
            }
        }

        void AllowUntrusted_Toggled(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }

            if (e.OriginalSource is ToggleSwitch toggleSwitch)
            {
                Settings.Instance.AllowUntrusted = toggleSwitch.IsOn;
                App.CurrentApp.MainWindow.FilterDLSSRecords();
            }
        }

        async Task CheckForUpdatesAsync()
        {
            IsCheckingForUpdates = true;
            var githubUpdater = new Data.GitHub.GitHubUpdater();
            var newUpdate = await githubUpdater.CheckForNewGitHubRelease();      
            if (newUpdate == null)
            {

                var dialog = new EasyContentDialog(XamlRoot)
                {
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "No new updates are available.",
                };
                await dialog.ShowAsync();

                IsCheckingForUpdates = false;
                return;
            }

            await githubUpdater.DisplayNewUpdateDialog(newUpdate, this);

            IsCheckingForUpdates = false;
        }

        private void LoggingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }

            if (e.AddedItems.Any() && e.AddedItems[0] is LoggingLevel loggingLevel && Settings.Instance.LoggingLevel != loggingLevel)
            {
                // Update settings
                Settings.Instance.LoggingLevel = loggingLevel;

                // Reconfigure
                Logger.ChangeLoggingLevel(loggingLevel);
            }
        }

        private async void LogFile_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            try
            {
                if (File.Exists(CurrentLogPath))
                {
                    Process.Start("explorer.exe", $"/select,{CurrentLogPath}");
                }
                else
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(CurrentLogPath));
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);

                var dialog = new EasyContentDialog(XamlRoot)
                {
                    Title = "Oops",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "Could not open your log file directly from DLSS Swapper. Please try open it manually.",
                };

                await dialog.ShowAsync();
            }
        }
    }
}
