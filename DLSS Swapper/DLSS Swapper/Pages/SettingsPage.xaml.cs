using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.ViewManagement;

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
                return String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();

            // Initilize defaults.
            LightThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Light;
            DarkThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Dark;
            DefaultThemeRadioButton.IsChecked = Settings.AppTheme == ElementTheme.Default;

            AllowUntrustedToggleSwitch.IsOn = Settings.AllowUntrusted;
            AllowExperimentalToggleSwitch.IsOn = Settings.AllowExperimental;
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
                    MainWindow.NavigationView.RequestedTheme = newTheme;
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
    }
}
