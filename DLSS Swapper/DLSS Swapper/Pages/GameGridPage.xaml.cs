using DLSS_Swapper.Data;
using DLSS_Swapper.Data.TechPowerUp;
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
        public ObservableCollection<Game> AllGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> FilteredGames { get; } = new ObservableCollection<Game>();
        List<LocalDll> _localDlls { get; } = new List<LocalDll>();
        List<TechPowerUpDllHash> _dlssHashes { get; }  = new List<TechPowerUpDllHash>();

        public GameGridPage()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        async Task LoadLocalDlls()
        {
            await Task.Run(() =>
            {
                var dlssDlls = Directory.GetFiles(Settings.DllsDirectory, "nvngx_dlss.dll", SearchOption.AllDirectories);
                foreach (var dlssDll in dlssDlls)
                {
                    // lol gross.
                    var directoryVersion = Path.GetFileName(Path.GetDirectoryName(dlssDll));
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                    var dlssVersion = $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}.{fileVersionInfo.FilePrivatePart}";

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
            var steamLibrary = new SteamLibrary();
            var steamGames = await steamLibrary.ListGamesAsync();

            steamGames.Sort();

            DispatcherQueue.TryEnqueue(() => {
                foreach (var game in steamGames)
                {
                    AllGames.Add(game);
                    FilteredGames.Add(game);
                }
            });
            
        }

        async Task LoadDllHashes()
        {
            var url = "https://gist.githubusercontent.com/beeradmoore/3467646864751964dbf22f462c2e5b1e/raw/techpowerup_dlss_dll_hashes.json";

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
                System.Diagnostics.Debug.WriteLine($"LoadDllHashes Error: {err.Message}");
            }
        }

        async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var tasks = new List<Task>();
            tasks.Add(LoadGamesAsync());
            tasks.Add(LoadDllHashes());
            tasks.Add(LoadLocalDlls());

            await Task.WhenAll(tasks);

            DispatcherQueue.TryEnqueue(() => {
                LoadingStackPanel.Visibility = Visibility.Collapsed;
            });
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

                if (game.HasDLSS == false)
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

                var dlssPickerPage = new DLSSPickerPage(game, _localDlls);
                dialog = new ContentDialog();
                dialog.Title = "Select DLSS Version";
                dialog.PrimaryButtonText = "Update";
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = dlssPickerPage;
                dialog.XamlRoot = this.XamlRoot;

                if (String.IsNullOrEmpty(game.BaseDLSSVersion) == false)
                {
                    dialog.SecondaryButtonText = "Reset";
                }

                var result = await dialog.ShowAsync();


                if (result == ContentDialogResult.Primary)
                {
                    var selectedDll = dlssPickerPage.GetSelectedLocalDll();
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

        private void UpdateFilteredList()
        {
            FilteredGames.Clear();
            foreach (Game currentGame in AllGames)
            {
                if (currentGame.HasDLSS && DLSSCheckbox.IsChecked == true)
                    FilteredGames.Add(currentGame);
                else if (!currentGame.HasDLSS && DLSSCheckbox.IsChecked == false)
                {
                    FilteredGames.Add(currentGame);
                }
                else if(DLSSCheckbox.IsChecked == null)
                {
                    FilteredGames.Add(currentGame);
                }
            }
        }

        private void DLSS_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateFilteredList();
        }

        private void DLSS_Checkbox_Indeterminate(object sender, RoutedEventArgs e)
        {
            UpdateFilteredList();
        }

        private void DLSS_Checkbox_UnChecked(object sender, RoutedEventArgs e)
        {
            UpdateFilteredList();
        }
    }
}
