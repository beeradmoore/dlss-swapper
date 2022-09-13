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

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// Page for application settings. A lot of this was taken from Xaml-Controls-Gallery, https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/SettingsPage.xaml
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        //https://github.com/microsoft/Xaml-Controls-Gallery/blob/6450265cc94da5b2fac5e1e22d1be35dc66c402e/XamlControlsGallery/Navigation/NavigationRootPage.xaml.cs#L32


        public string Version
        {
            get
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

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

        public IEnumerable<LoggingLevel> LoggingLevels = Enum.GetValues(typeof(LoggingLevel)).Cast<LoggingLevel>();

        public string CurrentLogPath => Logger.GetCurrentLogPath();

        public SettingsPage()
        {
            this.InitializeComponent();


            // Initilize defaults.
            LightThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Light;
            DarkThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Dark;
            DefaultThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Default;

            AllowUntrustedToggleSwitch.IsOn = Settings.AllowUntrusted;
            AllowExperimentalToggleSwitch.IsOn = Settings.AllowExperimental;
            LoggingComboBox.SelectedItem = Settings.LoggingLevel;

            DataContext = this;
        }

        void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
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

                    Settings.AppTheme = newTheme;
                    ((App)Application.Current)?.MainWindow?.UpdateColors(newTheme);
                }
            }
        }

        void AllowExperimental_Toggled(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ToggleSwitch toggleSwitch)
            {
                Settings.AllowExperimental = toggleSwitch.IsOn;
                App.CurrentApp.MainWindow.FilterDLSSRecords();
            }
        }

        void AllowUntrusted_Toggled(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ToggleSwitch toggleSwitch)
            {
                Settings.AllowUntrusted = toggleSwitch.IsOn;
                App.CurrentApp.MainWindow.FilterDLSSRecords();
            }
        }

        // We only check for updates for builds which are not from the Windows Store.
        async Task CheckForUpdatesAsync()
        {
#if !RELEASE_WINDOWSSTORE
            IsCheckingForUpdates = true;
            var githubUpdater = new Data.GitHub.GitHubUpdater();
            var newUpdate = await githubUpdater.CheckForNewGitHubRelease();      
            if (newUpdate == null)
            {

                var dialog = new ContentDialog()
                {
                    CloseButtonText = "Okay",
                    Content = "No new updates are available.",
                    XamlRoot = XamlRoot,
                };
                await dialog.ShowAsync();

                IsCheckingForUpdates = false;
                return;
            }

            await githubUpdater.DisplayNewUpdateDialog(newUpdate, this);

            IsCheckingForUpdates = false;
#endif
        }

        async void WindowsStoreBadge_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // This is a long mess and so much easier in xaml.
            var richTextBlock = new RichTextBlock();
            var paragraph = new Paragraph()
            {
                Margin = new Thickness(0, 0, 0, 0),
            };
            paragraph.Inlines.Add(new Run()
            {
                Text = "The recommended way to install DLSS Swapper is now via the Windows Store.",
            });
            richTextBlock.Blocks.Add(paragraph);
            paragraph = new Paragraph()
            {
                Margin = new Thickness(0, 12, 0, 0),
            };
            paragraph.Inlines.Add(new Run()
            {
                Text = "GitHub releases tab will still be updated with new releases, however the app will no longer silently update to the latest version. ",
            });
            richTextBlock.Blocks.Add(paragraph);
            paragraph = new Paragraph()
            {
                Margin = new Thickness(0, 12, 0, 0),
            };
            paragraph.Inlines.Add(new Run()
            {
                Text = "To transition to the Windows Store build it is recommended that you uninstall DLSS Swapper and its develeper certifciate. You can do this by following the ",
            });
            var hyperLink = new Hyperlink()
            {
                NavigateUri = new Uri("https://beeradmoore.github.io/dlss-swapper/uninstall/"),

            };
            hyperLink.Inlines.Add(new Run()
            {
                Text = "uninstall instructions"
            });
            paragraph.Inlines.Add(hyperLink);
            paragraph.Inlines.Add(new Run()
            {
                Text = " and then installing from the ",
            });
            hyperLink = new Hyperlink()
            {
                NavigateUri = new Uri("https://www.microsoft.com/store/apps/9NNL4H1PTJBL"),

            };
            hyperLink.Inlines.Add(new Run()
            {
                Text = "Windows Store"
            });
            paragraph.Inlines.Add(hyperLink);
            paragraph.Inlines.Add(new Run()
            {
                Text = ".",
            });
            richTextBlock.Blocks.Add(paragraph);
            paragraph = new Paragraph()
            {
                Margin = new Thickness(0, 12, 0, 0),
            };
            paragraph.Inlines.Add(new Run()
            {
                Text = "(open both links now as this dialog will close when you uninstall)",
            });
            richTextBlock.Blocks.Add(paragraph);

            var dialog = new ContentDialog()
            {
                Title = "DLSS Swapper is available on the Windows Store",
                CloseButtonText = "Okay",
                Content = richTextBlock,
                XamlRoot = XamlRoot,
            };
            
            await dialog.ShowAsync();
        }

        private void LoggingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Any() && e.AddedItems[0] is LoggingLevel loggingLevel && Settings.LoggingLevel != loggingLevel)
            {
                // Update settings
                Settings.LoggingLevel = loggingLevel;

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

                var dialog = new ContentDialog()
                {
                    Title = "Oops",
                    CloseButtonText = "Okay",
                    Content = "Could not open your log file directly from DLSS Swapper. Please try open it manually.",
                    XamlRoot = XamlRoot,
                };

                await dialog.ShowAsync();
            }
        }
    }
}
