using AsyncAwaitBestPractices;
using DLSS_Swapper.Data;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Pages;
using DLSS_Swapper.UserControls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        bool _isCustomizationSupported;
        ThemeWatcher _themeWatcher;
        IntPtr _windowIcon;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string iconPath, ref IntPtr index);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int DestroyIcon(IntPtr hIcon);

        public MainWindow()
        {
            this.InitializeComponent();
            LoadTranslation();

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

            _isCustomizationSupported = AppWindowTitleBar.IsCustomizationSupported();

            _themeWatcher = new ThemeWatcher();
            _themeWatcher.ThemeChanged += ThemeWatcher_ThemeChanged;
            _themeWatcher.Start();


            if (_isCustomizationSupported)
            {
                var appWindow = GetAppWindowForCurrentWindow();
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


            UpdateColors(Settings.Instance.AppTheme);

            //MainNavigationView.RequestedTheme = (ElementTheme)Settings.Instance.AppTheme;

            SetIcon();
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
                GoToPage("Settings");
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
            if (page == "Games")
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != gameGridPage)
                {
                    ContentFrame.Content = gameGridPage ??= new GameGridPage();
                }
            }
            else if (page == "Library")
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != libraryPage)
                {
                    ContentFrame.Content = libraryPage ??= new LibraryPage();
                }
            }
            else if (page == "Settings")
            {
                if (ContentFrame.Content is null || ContentFrame.Content as Page != settingsPage)
                {
                    ContentFrame.Content = settingsPage ??= new SettingsPage();
                }
            }
            else
            {
                Logger.Error($"Attempting to navigate to a page that was not found, {page}");
                return;
            }

            // Only try manually set selected item if is not already selected. 
            if (MainNavigationView.SelectedItem is null || (MainNavigationView.SelectedItem is string selectedItem && selectedItem != page))
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

        async void MainNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
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
            // Lazy blocks to allow mul
            var newUpdateTask = gitHubUpdater.CheckForNewGitHubRelease();


            // Load from cache, or download if not found.
            var loadDlssRecrodsTask = LoadDLLRecordsAsync();
            var loadImportedDlssRecordsTask = LoadImportedManifestAsync();


            if (Settings.Instance.HasShownMultiplayerWarning == false)
            {
                var dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                {
                    Title = "Note for multiplayer games",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "While swapping DLSS versions should not be considered cheating, certain anti-cheat systems may not be happy with you if the files in your game directory are not what the game was distributed with.\n\nBecause of this we recommend using caution for multiplayer games.",
                };
                var result = await dialog.ShowAsync();

                Settings.Instance.HasShownMultiplayerWarning = true;
            }


            var didLoadDlssRecords = await loadDlssRecrodsTask;
            if (didLoadDlssRecords == false)
            {
                var dialog = new EasyContentDialog(MainNavigationView.XamlRoot)
                {
                    Title = "Error",
                    CloseButtonText = "Close",
                    PrimaryButtonText = "Github Issues",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = @"We were unable to load manifest.json from your computer or from the internet. 

If this keeps happening please file an report in our issue tracker on Github.

DLSS Swapper will close now.",
                };
                var response = await dialog.ShowAsync();
                if (response == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues"));
                }

                Close();
            }

            await loadImportedDlssRecordsTask;

            FilterDLLRecords();
            //await App.CurrentApp.LoadLocalRecordsAsync();
            DLLManager.Instance.LoadLocalRecords();

            // We are now ready to show the games list.
            LoadingStackPanel.Visibility = Visibility.Collapsed;
            GoToPage("Games");


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

        /// <summary>
        /// Attempts to load DLSS records from disk or from the web depending what happened.
        /// </summary>
        /// <returns>True if we expect there are now valid DLSS records loaded into memory.</returns>
        // Previously LoadDLSSRecordsAsync
        async Task<bool> LoadDLLRecordsAsync()
        {
            // Only auto check for updates once every 12 hours.
            var timeSinceLastUpdate = DateTimeOffset.Now - Settings.Instance.LastRecordsRefresh;
            if (timeSinceLastUpdate.TotalMinutes > 5)
            {
                var didUpdate = await UpdateManifestAsync();
                if (didUpdate)
                {
                    // If we did upda
                    return true;
                }
            }

            try
            {
                // If we were unable to auto-load lets try load cached.
                var manifest = await Storage.LoadManifestJsonAsync();

                // If manifest could not be loaded then we should attempt to upload dlss_records from the dlss-archive.
                if (manifest is null || manifest.DLSS?.Any() == false)
                {
                    return await UpdateManifestAsync();
                }
                else
                {
                    DLLManager.Instance.UpdateDLLRecordLists(manifest);
                }
                return true;
            }
            catch (Exception err)
            {
                Logger.Error(err);
                return false;
            }
        }

        // Previously: LoadImportedDLSSRecordsAsync
        internal async Task LoadImportedManifestAsync()
        {
            var manifest = await Storage.LoadImportedManifestJsonAsync();
            if (manifest is not null)
            {
                UpdateImportedManifestList(manifest);
            }
        }

        // Previously: UpdateImportedDLSSRecordsList
        internal void UpdateImportedManifestList(Manifest importedManifest)
        {
            // TODO: Reimplement
            /*
            App.CurrentApp.ImportedDLSSRecords.Clear();
            App.CurrentApp.ImportedDLSSRecords.AddRange(localDlssRecords);
            */
        }

        /// <summary>
        /// Attempts to load manifest.json from dlss-swapper-manifest-builder repository.
        /// </summary>
        /// <returns>True if the dlss recrods manifest was downloaded and saved successfully</returns>
        internal async Task<bool> UpdateManifestAsync()
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // TODO: Check how quickly this takes to timeout if there is no internet connection. Consider 
                    // adding a "fast UpdateManifest" which will quit early if we were unable to load in 10sec 
                    // which would then fall back to loading local.
                    var fileDownloader = new FileDownloader("https://raw.githubusercontent.com/beeradmoore/dlss-swapper-manifest-builder/refs/heads/main/manifest.json", 0);
                    await fileDownloader.DownloadFileToStreamAsync(memoryStream);

                    memoryStream.Position = 0;

                    var manifest = await JsonSerializer.DeserializeAsync(memoryStream, SourceGenerationContext.Default.Manifest);
                    if (manifest is null)
                    {
                        throw new Exception("Could not deserialize manifest.json.");
                    }
                    DLLManager.Instance.UpdateDLLRecordLists(manifest);
                    //await UpdateDLSSRecordsListAsync(items);

                    memoryStream.Position = 0;
                    try
                    {
                        await Storage.SaveManifestJsonAsync(manifest);
                        // Update settings for auto refresh.
                        Settings.Instance.LastRecordsRefresh = DateTime.Now;
                        return true;
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err);
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
                Debugger.Break();
            }

            return false;
        }


        internal void UpdateColors(ElementTheme theme)
        {
            ((App)Application.Current).GlobalElementTheme = theme;

            if (theme == ElementTheme.Light)
            {
                UpdateColorsLight();
            }
            else if (theme == ElementTheme.Dark)
            {
                UpdateColorsDark();
            }
            else
            {
                var osApplicationTheme = _themeWatcher.GetWindowsApplicationTheme();
                if (osApplicationTheme == ApplicationTheme.Light)
                {
                    UpdateColorsLight();
                }
                else if (osApplicationTheme == ApplicationTheme.Dark)
                {
                    UpdateColorsDark();
                }
            }
        }

        void UpdateColorsLight()
        {
            App.CurrentApp.RunOnUIThread(() =>
            {
                RootGrid.RequestedTheme = ElementTheme.Light;


                var app = ((App)Application.Current);
                var theme = app.Resources.MergedDictionaries[1].ThemeDictionaries["Light"] as ResourceDictionary;

                if (theme is null)
                {
                    return;
                }

                if (_isCustomizationSupported)
                {
                    var appWindow = GetAppWindowForCurrentWindow();
                    var appWindowTitleBar = appWindow.TitleBar;


                    appWindowTitleBar.ButtonBackgroundColor = (Color)theme["ButtonBackgroundColor"];
                    appWindowTitleBar.ButtonForegroundColor = (Color)theme["ButtonForegroundColor"];
                    appWindowTitleBar.ButtonHoverBackgroundColor = (Color)theme["ButtonHoverBackgroundColor"];
                    appWindowTitleBar.ButtonHoverForegroundColor = (Color)theme["ButtonHoverForegroundColor"];
                    appWindowTitleBar.ButtonInactiveBackgroundColor = (Color)theme["ButtonInactiveBackgroundColor"];
                    appWindowTitleBar.ButtonInactiveForegroundColor = (Color)theme["ButtonInactiveForegroundColor"];
                    appWindowTitleBar.ButtonPressedBackgroundColor = (Color)theme["ButtonPressedBackgroundColor"];
                    appWindowTitleBar.ButtonPressedForegroundColor = (Color)theme["ButtonPressedForegroundColor"];

                }
                else
                {
                    var appResources = Application.Current.Resources;
                    // Removes the tint on title bar
                    appResources["WindowCaptionBackground"] = theme["WindowCaptionBackground"];
                    appResources["WindowCaptionBackgroundDisabled"] = theme["WindowCaptionBackgroundDisabled"];
                    // Sets the tint of the forground of the buttons
                    appResources["WindowCaptionForeground"] = theme["WindowCaptionForeground"];
                    appResources["WindowCaptionForegroundDisabled"] = theme["WindowCaptionForegroundDisabled"];

                    appResources["WindowCaptionButtonBackgroundPointerOver"] = theme["WindowCaptionButtonBackgroundPointerOver"];


                    RepaintCurrentWindow();
                }
            });
        }

        void UpdateColorsDark()
        {
            App.CurrentApp.RunOnUIThread(() =>
            {
                RootGrid.RequestedTheme = ElementTheme.Dark;

                var app = ((App)Application.Current);
                var theme = app.Resources.MergedDictionaries[1].ThemeDictionaries["Dark"] as ResourceDictionary;

                if (theme is null)
                {
                    return;
                }

                if (_isCustomizationSupported)
                {
                    var appWindow = GetAppWindowForCurrentWindow();
                    var appWindowTitleBar = appWindow.TitleBar;

                    appWindowTitleBar.ButtonBackgroundColor = (Color)theme["ButtonBackgroundColor"];
                    appWindowTitleBar.ButtonForegroundColor = (Color)theme["ButtonForegroundColor"];
                    appWindowTitleBar.ButtonHoverBackgroundColor = (Color)theme["ButtonHoverBackgroundColor"];
                    appWindowTitleBar.ButtonHoverForegroundColor = (Color)theme["ButtonHoverForegroundColor"];
                    appWindowTitleBar.ButtonInactiveBackgroundColor = (Color)theme["ButtonInactiveBackgroundColor"];
                    appWindowTitleBar.ButtonInactiveForegroundColor = (Color)theme["ButtonInactiveForegroundColor"];
                    appWindowTitleBar.ButtonPressedBackgroundColor = (Color)theme["ButtonPressedBackgroundColor"];
                    appWindowTitleBar.ButtonPressedForegroundColor = (Color)theme["ButtonPressedForegroundColor"];
                }
                else
                {
                    var appResources = Application.Current.Resources;

                    // Removes the tint on title bar
                    appResources["WindowCaptionBackground"] = theme["WindowCaptionBackground"];
                    appResources["WindowCaptionBackgroundDisabled"] = theme["WindowCaptionBackgroundDisabled"];
                    // Sets the tint of the forground of the buttons
                    appResources["WindowCaptionForeground"] = theme["WindowCaptionForeground"];
                    appResources["WindowCaptionForegroundDisabled"] = theme["WindowCaptionForegroundDisabled"];

                    appResources["WindowCaptionButtonBackgroundPointerOver"] = theme["WindowCaptionButtonBackgroundPointerOver"];

                    RepaintCurrentWindow();
                }
            });
        }

        AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        // to trigger repaint tracking task id 38044406
        void RepaintCurrentWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            var activeWindow = Win32.GetActiveWindow();
            if (hWnd == activeWindow)
            {
                Win32.SendMessage(hWnd, Win32.WM_ACTIVATE, Win32.WA_INACTIVE, IntPtr.Zero);
                Win32.SendMessage(hWnd, Win32.WM_ACTIVATE, Win32.WA_ACTIVE, IntPtr.Zero);
            }
            else
            {
                Win32.SendMessage(hWnd, Win32.WM_ACTIVATE, Win32.WA_ACTIVE, IntPtr.Zero);
                Win32.SendMessage(hWnd, Win32.WM_ACTIVATE, Win32.WA_INACTIVE, IntPtr.Zero);
            }
        }

        void ThemeWatcher_ThemeChanged(object? sender, ApplicationTheme e)
        {
            var globalTheme = ((App)Application.Current).GlobalElementTheme;

            if (globalTheme == ElementTheme.Default)
            {
                var osApplicationTheme = _themeWatcher.GetWindowsApplicationTheme();

                if (osApplicationTheme == ApplicationTheme.Light)
                {
                    UpdateColorsLight();
                }
                else if (osApplicationTheme == ApplicationTheme.Dark)
                {
                    UpdateColorsDark();
                }
            }
        }

        private void LoadTranslation()
        {
            Title = ResourceHelper.GetString("ApplicationTitle");
            AppTitle.Text = ResourceHelper.GetString("ApplicationTitle");
            NavigationViewItemGames.Content = ResourceHelper.GetString("Games");
            NavigationViewItemLibrary.Content = ResourceHelper.GetString("Library");
            LoadingProgressText.Text = ResourceHelper.GetString("Loading");
        }
    }
}
