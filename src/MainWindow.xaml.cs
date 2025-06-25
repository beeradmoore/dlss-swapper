using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Pages;
using DLSS_Swapper.UserControls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;

namespace DLSS_Swapper
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindowModel ViewModel { get; private set; }

        IntPtr _windowIcon;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string iconPath, ref IntPtr index);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int DestroyIcon(IntPtr hIcon);

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainWindowModel();

            if (AppWindow?.Presenter is OverlappedPresenter overlappedPresenter)
            {
                var lastWindowSizeAndPosition = Settings.Instance.LastWindowSizeAndPosition;
                if (lastWindowSizeAndPosition.Width > 512 && lastWindowSizeAndPosition.Height > 512)
                {
                    AppWindow.MoveAndResize(lastWindowSizeAndPosition.GetRectInt32());
                }
                if (lastWindowSizeAndPosition.State == OverlappedPresenterState.Maximized)
                {
                    overlappedPresenter.Maximize();
                }
            }

            Closed += (object sender, WindowEventArgs args) =>
            {
                if (AppWindow?.Size is not null && AppWindow?.Position is not null && AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    var windowPositionRect = new WindowPositionRect(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);
                    if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
                    {
                        windowPositionRect.State = OverlappedPresenterState.Maximized;
                    }
                    Settings.Instance.LastWindowSizeAndPosition = windowPositionRect;
                }

                // Release the icon.
                if (_windowIcon != IntPtr.Zero)
                {
                    DestroyIcon(_windowIcon);
                    _windowIcon = IntPtr.Zero;
                }
            };

            if (WindowManager.IsCustomizationSupported)
            {
                var appWindow = App.CurrentApp.WindowManager.GetAppWindowForWindow(this);
                var appWindowTitleBar = appWindow.TitleBar;
                appWindowTitleBar.ExtendsContentIntoTitleBar = true;
                RootGrid.RowDefinitions[0].Height = new GridLength(32);
            }
            else
            {
                RootGrid.RowDefinitions[0].Height = new GridLength(28);
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }

            SetIcon();

            // Update settings text when language changes.
            LanguageManager.Instance.OnLanguageChanged += () =>
            {
                if (MainNavigationView.SettingsItem is NavigationViewItem settingsNavigationViewItem)
                {
                    settingsNavigationViewItem.Content = ResourceHelper.GetString("SettingsPage_Title");
                }
            };
        }



        /// <summary>
        /// Default the Window Icon to the icon stored in the .exe, if any.
        ///
        /// The Icon can be overriden by callers by calling SetIcon themselves.
        /// </summary>
        /// via this MAUI PR https://github.com/dotnet/maui/pull/6900
        void SetIcon()
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                var index = IntPtr.Zero; // 0 = first icon in resources
                _windowIcon = ExtractAssociatedIcon(IntPtr.Zero, processPath, ref index);
                if (_windowIcon != IntPtr.Zero)
                {
                    var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

                    var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(windowHandle));
                    if (appWindow is not null)
                    {
                        var iconId = Win32Interop.GetIconIdFromIcon(_windowIcon);
                        appWindow.SetIcon(iconId);
                    }
                }
            }
        }



        void MainNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                GoToPage(SettingsPage.PageTag);
            }
            else if (args.InvokedItemContainer.Tag is string invokedItem)
            {
                GoToPage(invokedItem);
            }
            else
            {
                Logger.Error($"MainNavigationView_ItemInvoked for a page that was not found. ({args.InvokedItemContainer?.Tag}, {args.InvokedItem})");
            }
        }


        GameGridPage? gameGridPage = null;
        LibraryPage? libraryPage = null;
        SettingsPage? settingsPage = null;

        public GameGridPage? GameGridPage => gameGridPage;

        void GoToPage(string page)
        {
            ViewModel.AcknowledgementsVisibility = Visibility.Collapsed;

            if (page == GameGridPage.PageTag)
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != gameGridPage)
                {
                    ContentFrame.Content = gameGridPage ??= new GameGridPage();
                }
            }
            else if (page == LibraryPage.PageTag)
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != libraryPage)
                {
                    ContentFrame.Content = libraryPage ??= new LibraryPage();
                }
            }
            else if (page == SettingsPage.PageTag)
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != settingsPage)
                {
                    ContentFrame.Content = settingsPage ??= new SettingsPage();
                }
            }
            else if (page ==  AcknowledgementsPage.PageTag)
            {
                if (ContentFrame.Content is null || ContentFrame.Content is not AcknowledgementsPage)
                {
                    ViewModel.AcknowledgementsVisibility = Visibility.Visible;
                    ContentFrame.Content = new AcknowledgementsPage();
                }
            }
            else
            {
                Logger.Error($"Attempting to navigate to a page that was not found, {page}");
                return;
            }

            // Only try manually set selected item if is not already selected.
            if (MainNavigationView.SelectedItem is null || (MainNavigationView.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag.ToString() != page))
            {
                foreach (NavigationViewItem navigationViewItem in MainNavigationView.MenuItems)
                {
                    if (navigationViewItem.Tag.ToString() == page)
                    {
                        MainNavigationView.SelectedItem = navigationViewItem;
                        break;
                    }
                }
            }
        }

        internal void GoToAcknowledgements()
        {
            GoToPage(AcknowledgementsPage.PageTag);
        }

        async void MainNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NavigationView navigationView && navigationView.SettingsItem is NavigationViewItem settingsNavigationViewItem)
            {
                settingsNavigationViewItem.Tag = SettingsPage.PageTag;
                settingsNavigationViewItem.Content = ResourceHelper.GetString("SettingsPage_Title");
            }


            // TODO: Disabled because CommunityToolkit.WinUI.Helpers.SystemInformation.Instance.IsAppUpdated throws exceptions for unpackaged apps.
            /*
            // If this is a new build, fetch updates to display to the user.
            Task<Data.GitHub.GitHubRelease> releaseNotesTask = null;
            if (CommunityToolkit.WinUI.Helpers.SystemInformation.Instance.IsAppUpdated)
            {
                var currentAppVersion = App.CurrentApp.GetVersion();
                releaseNotesTask = gitHubUpdater.GetReleaseFromTag($"v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}.{currentAppVersion.Revision}");
            }
            */

            var gitHubUpdater = new Data.GitHub.GitHubUpdater();

            // If this is a GitHub build check if there is a new version.
            var newUpdateTask = gitHubUpdater.CheckForNewGitHubRelease(false);

            await DLLManager.Instance.LoadManifestsAsync();


            if (Settings.Instance.HasShownMultiplayerWarning == false)
            {
                var dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                {
                    Title = ResourceHelper.GetString("MainWindow_NoteForMultiplayerGames_Title"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("MainWindow_NoteForMultiplayerGames_Message"),
                };
                var result = await dialog.ShowAsync();

                Settings.Instance.HasShownMultiplayerWarning = true;
            }


            if (DLLManager.Instance.HasLoadedManifest() == false)
            {
                var dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                    PrimaryButtonText = ResourceHelper.GetString("MainWindow_ManifestCouldNotBeLoaded_GitHubIssues"),
                    SecondaryButtonText = ResourceHelper.GetString("MainWindow_ManifestCouldNotBeLoaded_UpdateManifest"),
                    DefaultButton = ContentDialogButton.Primary,
                    Content = ResourceHelper.GetString("MainWindow_ManifestCouldNotBeLoaded_Message"),
                };
                var shouldClose = true;

                var response = await dialog.ShowAsync();
                if (response == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues"));
                }
                else if (response is ContentDialogResult.Secondary)
                {
                    dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                    {
                        Title = ResourceHelper.GetString("MainWindow_AttemptingManifestUpdate"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = new ProgressRing()
                        {
                            IsActive = true,
                            IsIndeterminate = true,
                        },
                    };

                    var updateTask = DLLManager.Instance.UpdateManifestAsync();
                    _ = dialog.ShowAsync();
                    await updateTask;
                    dialog.Hide();

                    if (DLLManager.Instance.HasLoadedManifest() == true)
                    {
                        shouldClose = false;
                    }
                }

                if (shouldClose)
                {
                    dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                    {
                        Title = ResourceHelper.GetString("MainWindow_DlssSwapperMustClose"),
                        CloseButtonText = ResourceHelper.GetString("General_Close"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = ResourceHelper.GetString("MainWindow_DlssSwapperCloseDueToManifest"),
                    };
                    await dialog.ShowAsync();

                    Close();
                }
            }

            if (DLLManager.Instance.ImportedManifest is null)
            {
                var dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                {
                    Title = ResourceHelper.GetString("LibraryPage_CouldNotLoadImportedDlls"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = new ImportSystemDisabledView(),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                };
                await dialog.ShowAsync();
            }

            //FilterDLLRecords();

            // Yeet this into the void and let it load in the background.
            _ = DLLManager.Instance.UpdateManifestIfOldAsync();

            // We are now ready to show the games list.
            LoadingStackPanel.Visibility = Visibility.Collapsed;

            GoToPage(GameGridPage.PageTag);

            // TODO: Disabled because CommunityToolkit.WinUI.Helpers.SystemInformation.Instance.IsAppUpdated throws exceptions for unpackaged apps.
            /*
            if (releaseNotesTask is not null)
            {
                await releaseNotesTask;
                if (releaseNotesTask.Result is not null)
                {
                    gitHubUpdater?.DisplayWhatsNewDialog(releaseNotesTask.Result, MainNavigationView);
                }
            }
            */

            // TODO: What happens if you have no internet
            await newUpdateTask;
            if (newUpdateTask.Result is not null)
            {
                if (gitHubUpdater.HasPromptedBefore(newUpdateTask.Result) == false)
                {
                    await gitHubUpdater.DisplayNewUpdateDialog(newUpdateTask.Result, MainNavigationView.XamlRoot);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        // Previously: FilterDLSSRecords
        internal void FilterDLLRecords()
        {
            // TODO: Reimplement
            /*
            var newDlssRecordsList = new List<DLLRecord>();
            if (Settings.Instance.AllowUntrusted)
            {
                newDlssRecordsList.AddRange(App.CurrentApp.Manifest.DLSS);
                newDlssRecordsList.AddRange(App.CurrentApp.ImportedManifest.DLSS);
            }
            else
            {
                newDlssRecordsList.AddRange(App.CurrentApp.Manifest.DLSS.Where(x => x.IsSignatureValid == true));
                newDlssRecordsList.AddRange(App.CurrentApp.ImportedManifest.DLSS.Where(x => x.IsSignatureValid == true));
            }

            newDlssRecordsList.Sort();
            CurrentDLSSRecords.Clear();
            CurrentDLSSRecords.AddRange(newDlssRecordsList);
            */

        }
    }
}
