using DLSS_Swapper.Data;
using DLSS_Swapper.Data.TechPowerUp;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameGridPage : Page
    {
        public List<IGameLibrary> GameLibraries { get; } = new List<IGameLibrary>();

        List<LocalDll> _localDlls { get; } = new List<LocalDll>();
        List<TechPowerUpDllHash> _dlssHashes { get; }  = new List<TechPowerUpDllHash>();

        bool _loadingGamesAndDlls = false;

        ObservableCollection<Game> games = new ObservableCollection<Game>();

        public GameGridPage()
        {
            this.InitializeComponent();
            DataContext = this;
            MainGridView.ItemsSource = games;
        }

        async Task LoadLocalDlls()
        {
            await Task.Run(() =>
            {
                _localDlls.Clear();
                var dlssDlls = Directory.GetFiles(Settings.DllsDirectory, "nvngx_dlss.dll", SearchOption.AllDirectories);
                foreach (var dlssDll in dlssDlls)
                {
                    var directoryVersion = new Version(Path.GetFileName(Path.GetDirectoryName(dlssDll)));
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                    var dlssVersion = new Version(fileVersionInfo.FileVersion.Replace(',', '.'));

                    // TODO : Validate with hash?. But how can we secure valid hashes?
                    if (directoryVersion == dlssVersion)
                    {
                        _localDlls.Add(new LocalDll(dlssDll));
                    }
                    _localDlls.Sort();
                }
            });
        }


        async Task LoadGamesAsync()
        {
            GameLibraries.Clear();

            // TODO: Settings to enable/disable game libraries.

            games.Clear();
            GC.Collect();

            var progress = new Progress<Game>(game =>
            {
                //skip game if user chose to hide non-DLSS games
                if (Settings.HideNonDLSSGames && !game.HasDLSS)
                {
                    return;
                }

                //insert game in correct order (alphabetically)
                int i = 0;
                while (i < games.Count && Comparer<Game>.Default.Compare(games[i], game) < 0)
                {
                    i++;
                }

                games.Insert(i, game);
            });

            List<Task> loadGameLibraries = new List<Task>();

            var steamLibrary = new SteamLibrary();
            GameLibraries.Add(steamLibrary);
            loadGameLibraries.Add(steamLibrary.ListGamesAsync(progress));

            var ubisoftLibrary = new UbisoftLibrary();
            GameLibraries.Add(ubisoftLibrary);
            loadGameLibraries.Add(ubisoftLibrary.ListGamesAsync(progress));

            await Task.WhenAll(loadGameLibraries);

        }

        async Task LoadDllHashes()
        {
            string url = "https://gist.githubusercontent.com/beeradmoore/3467646864751964dbf22f462c2e5b1e/raw/techpowerup_dlss_dll_hashes.json";

            try
            {
                using var client = new HttpClient();
                using var stream = await client.GetStreamAsync(url);

                var items = await JsonSerializer.DeserializeAsync<List<TechPowerUpDllHash>>(stream);
                _dlssHashes.Clear();
                _dlssHashes.AddRange(items);
            }
            catch (Exception err)
            {
                Debug.WriteLine($"LoadDllHashes Error: {err.Message}");
            }
        }

        async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGamesAndDlls();
        }

        async void MainGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            MainGridView.SelectedIndex = -1;
            if (e.AddedItems[0] is Game game)
            {
                ContentDialog dialog;

                if (!game.HasDLSS)
                {
                    dialog = new ContentDialog();
                    //dialog.Title = "Error";
                    dialog.PrimaryButtonText = "Okay";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.Content = $"DLSS was not detected in {game.Title}.";
                    dialog.XamlRoot = this.XamlRoot;
                    await dialog.ShowAsync();
                    return;
                }

                var dlssPickerControl = new DLSSPickerControl(game, _localDlls);
                dialog = new ContentDialog();
                dialog.Title = "Select DLSS Version";
                dialog.PrimaryButtonText = "Update";
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = dlssPickerControl;
                dialog.XamlRoot = this.XamlRoot;
                
                if (game.BaseDLSSVersion != null)
                {
                    dialog.SecondaryButtonText = "Reset";
                }

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var selectedDll = dlssPickerControl.GetSelectedLocalDll();
                    bool didUpdate = game.UpdateDll(selectedDll);

                    if (didUpdate == false)
                    {
                        dialog = new ContentDialog();
                        dialog.Title = "Error";
                        dialog.PrimaryButtonText = "Okay";
                        dialog.DefaultButton = ContentDialogButton.Primary;
                        dialog.Content = "Unable to update DLSS dll. You may need to repair your game manually.";
                        dialog.XamlRoot = this.XamlRoot;
                        await dialog.ShowAsync();
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    bool didReset = game.ResetDll();

                    if (didReset == false)
                    {
                        dialog = new ContentDialog();
                        dialog.Title = "Error";
                        dialog.PrimaryButtonText = "Okay";
                        dialog.DefaultButton = ContentDialogButton.Primary;
                        dialog.Content = "Unable to reset to default. Please repair your game manually.";
                        dialog.XamlRoot = this.XamlRoot;
                        await dialog.ShowAsync();
                    }
                }
            }
        }

        async Task LoadGamesAndDlls()
        {
            if (_loadingGamesAndDlls)
                return;

            _loadingGamesAndDlls = true;

            LoadingStackPanel.Visibility = Visibility.Visible;
            DisplayLoadingScreen.Begin();

            var tasks = new List<Task>();
            tasks.Add(LoadGamesAsync());
            tasks.Add(LoadDllHashes());
            tasks.Add(LoadLocalDlls());

            await Task.WhenAll(tasks);

            DispatcherQueue.TryEnqueue(() => {
                HideLoadingScreen.Begin();
                _loadingGamesAndDlls = false;
            });
        }

        async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadGamesAndDlls();
        }

        async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var gameFilterControl = new GameFilterControl();

            var dialog = new ContentDialog();
            dialog.Title = "Filter";
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = gameFilterControl;
            dialog.XamlRoot = this.XamlRoot;


            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Settings.HideNonDLSSGames = gameFilterControl.IsHideNonDLSSGamesChecked();
                Settings.GroupGameLibrariesTogether = gameFilterControl.IsGroupGameLibrariesTogetherChecked();

               await LoadGamesAndDlls();
            }
        }

        private void FadeOutThemeAnimation_Completed(object sender, object e)
        {
            LoadingStackPanel.Visibility = Visibility.Collapsed;
        }
    }
}
