using AsyncAwaitBestPractices;
using DLSS_Swapper.Data.TechPowerUp;
using DLSS_Swapper.Pages;
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
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static NavigationView NavigationView;
        public MainWindow()
        {
            Title = "DLSS Swapper [ beta ]";
            this.InitializeComponent();
            NavigationView = MainNavigationView;

            MainNavigationView.RequestedTheme = (ElementTheme)Settings.AppTheme;
        }

        void MainNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            //FrameNavigationOptions navOptions = new FrameNavigationOptions();
            //navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;

            if (args.InvokedItem is String invokedItem)
            {
                GoToPage(invokedItem);
            }
        }

        void GoToPage(string page)
        {
            Type pageType = null;

            if (page == "Games")
            {
                pageType = typeof(GameGridPage);
            }
            else if (page == "Download")
            {
                pageType = typeof(DownloadPage);
            }
            else if (page == "Settings")
            {
                pageType = typeof(SettingsPage);
            }

            foreach (NavigationViewItem navigationViewItem in MainNavigationView.MenuItems)
            {
                if (navigationViewItem.Tag.ToString() == page)
                {
                    MainNavigationView.SelectedItem = navigationViewItem;
                    break;
                }
            }

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }

        async void MainNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            GoToPage("Games");

            if (Settings.HasShownWorkInProgress == false)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Work in Progress - Please Read",
                    CloseButtonText = "Okay",
                    Content = @"DLSS Swapper not complete. This is an early beta, as such it may be somewhat confusing and not user friendly in its operation. 

For more details on how to use the tool please see the 'Usage' section of our site.",
                    PrimaryButtonText = "View Usage",
                    XamlRoot = MainNavigationView.XamlRoot,
                };
                var didClick = await dialog.ShowAsync();

                Settings.HasShownWorkInProgress = true;

                if (didClick == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri("https://beeradmoore.github.io/dlss-swapper/usage/"));
                }
            }

            if (Settings.HasShownWarning == false)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Warning",
                    CloseButtonText = "Okay",
                    Content = @"Replacing dlls on your computer can be dangerous.

Placing a malicious dll into a game is just as bad as running Linking_park_-_nUmB_mp3.exe that you just downloaded from LimeWire.

More protections and validations will come in a future update.",
                    XamlRoot = MainNavigationView.XamlRoot,
                };
                await dialog.ShowAsync();

                Settings.HasShownWarning = true;
            }



        }
    }
}
