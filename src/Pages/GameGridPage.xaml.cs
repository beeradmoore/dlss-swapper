using DLSS_Swapper.Data;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
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
using System.Security.Principal;
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

        bool _loadingGamesAndDlls = false;

        public GameGridPage()
        {
            this.InitializeComponent();
            DataContext = this;

        }


        async Task LoadGamesAsync()
        {
            // Added this check so if we get to here and this is true we probably crashed loading games last time and we should prompt for that.
            if (Settings.Instance.WasLoadingGames)
            {
                var richTextBlock = new RichTextBlock();
                var paragraph = new Paragraph()
                {
                    Margin = new Thickness(0, 0, 0, 0),
                };
                paragraph.Inlines.Add(new Run()
                {
                    Text = "DLSS Swapper had an issue loading game libraries.  Please try disabling a game library below. You can re-enable these options later in the settings.",
                });
                richTextBlock.Blocks.Add(paragraph);
                paragraph = new Paragraph()
                {
                    Margin = new Thickness(0, 0, 0, 0),
                };
                paragraph.Inlines.Add(new Run()
                {
                    Text = "If this keeps happening please file a bug report ",
                });
                var hyperLink = new Hyperlink()
                {
                    NavigateUri = new Uri("https://github.com/beeradmoore/dlss-swapper/issues"),

                };
                hyperLink.Inlines.Add(new Run()
                {
                    Text = "here"
                });
                paragraph.Inlines.Add(hyperLink);
                paragraph.Inlines.Add(new Run()
                {
                    Text = ".",
                });
                richTextBlock.Blocks.Add(paragraph);




                var grid = new Grid()
                {
                    RowSpacing = 10,
                };
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());

                Grid.SetRow(richTextBlock, 0);
                grid.Children.Add(richTextBlock);


                var gameLibrarySelectorControl = new GameLibrarySelectorControl();

                Grid.SetRow(gameLibrarySelectorControl, 1);
                grid.Children.Add(gameLibrarySelectorControl);


                var dialog = new EasyContentDialog(XamlRoot)
                {
                    Title = "Failed to load game libraries",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = grid,
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    gameLibrarySelectorControl.Save();
                }
            }

            Settings.Instance.WasLoadingGames = true;

            GameLibraries.Clear();


            // Auto game library loading.
            // Simply adding IGameLibrary interface means we will load the games.
            var loadGameTasks = new List<Task>(); 
            foreach (GameLibrary gameLibraryEnum in Enum.GetValues(typeof(GameLibrary)))
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                if (gameLibrary.IsEnabled())
                {
                    GameLibraries.Add(gameLibrary);
                    loadGameTasks.Add(gameLibrary.ListGamesAsync());
                }
            }

            // Await them all to finish loading games.
            await Task.WhenAll(loadGameTasks);

            Settings.Instance.WasLoadingGames = false;

            DispatcherQueue.TryEnqueue(() =>
            {
                FilterGames();
            });
        }

        void FilterGames()
        {
            // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
            MainGridView.SelectionChanged -= MainGridView_SelectionChanged;

            //MainGridView.ItemsSource = null;

            if (Settings.Instance.GroupGameLibrariesTogether)
            {

                var collectionViewSource = new CollectionViewSource()
                {
                    IsSourceGrouped = true,
                    Source = GameLibraries,
                };

                if (Settings.Instance.HideNonDLSSGames)
                {
                    collectionViewSource.ItemsPath = new PropertyPath("LoadedDLSSGames");
                }
                else
                {
                    collectionViewSource.ItemsPath = new PropertyPath("LoadedGames");
                }

                MainGridView.ItemsSource = collectionViewSource.View;
            }
            else
            {
                var games = new List<Game>();

                if (Settings.Instance.HideNonDLSSGames)
                {
                    foreach (var gameLibrary in GameLibraries)
                    {
                        games.AddRange(gameLibrary.LoadedGames.Where(g => g.HasDLSS == true));
                    }
                }
                else
                {
                    foreach (var gameLibrary in GameLibraries)
                    {
                        games.AddRange(gameLibrary.LoadedGames);
                    }
                }

                games.Sort();

                MainGridView.ItemsSource = games;
            }

            // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
            MainGridView.SelectedIndex = -1;
            MainGridView.SelectionChanged += MainGridView_SelectionChanged;
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
                EasyContentDialog dialog;

                if (game.HasDLSS == false)
                {
                    dialog = new EasyContentDialog(XamlRoot)
                    {
                        //dialog.Title = "Error";
                        PrimaryButtonText = "Okay",
                        DefaultButton = ContentDialogButton.Primary,
                        Content = $"DLSS was not detected in {game.Title}.",
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var dlssPickerControl = new DLSSPickerControl(game);
                dialog = new EasyContentDialog(XamlRoot)
                { 
                    Title = "Select DLSS Version",
                    PrimaryButtonText = "Swap",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = dlssPickerControl,
                };

                if (String.IsNullOrEmpty(game.BaseDLSSVersion) == false)
                {
                    dialog.SecondaryButtonText = "Reset";
                }

                var result = await dialog.ShowAsync();


                if (result == ContentDialogResult.Primary)
                {
                    var selectedDLSSRecord = dlssPickerControl.GetSelectedDLSSRecord();

                    if (selectedDLSSRecord.LocalRecord.IsDownloading == true || selectedDLSSRecord.LocalRecord.IsDownloaded == false)
                    {
                        // TODO: Initiate download here.
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            CloseButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Close,
                            Content = "Please download the DLSS record from the downloads page first.",
                        };
                        await dialog.ShowAsync();
                        return;
                    }

                    var didUpdate = game.UpdateDll(selectedDLSSRecord);

                    if (didUpdate.Success == false)
                    {
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            PrimaryButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Primary,
                            Content = didUpdate.Message,
                        };
                        /*
                        // Disabled as I am unsure how to prompt to run as admin.
                        if (didUpdate.PromptToRelaunchAsAdmin == true)
                        {
                            dialog.SecondaryButtonText = "Relaunch as Administrator";
                        }
                        */
                        var dialogResult = await dialog.ShowAsync();
                        /*
                        // Disabled as I am unsure how to prompt to run as admin.
                        if (didUpdate.PromptToRelaunchAsAdmin == true && dialogResult == ContentDialogResult.Secondary)
                        {
                            App.CurrentApp.RelaunchAsAdministrator();
                        }
                        */
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    var didReset = game.ResetDll();

                    if (didReset.Success == false)
                    {
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            PrimaryButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Primary,
                            Content = didReset.Message,
                        };
                        /*
                        // Disabled as I am unsure how to prompt to run as admin.
                        if (didReset.PromptToRelaunchAsAdmin == true)
                        {
                            dialog.SecondaryButtonText = "Relaunch as Administrator";
                        }
                        */
                        var dialogResult = await dialog.ShowAsync();/*
                        // Disabled as I am unsure how to prompt to run as admin.
                        if (didReset.PromptToRelaunchAsAdmin == true && dialogResult == ContentDialogResult.Secondary)
                        {
                            App.CurrentApp.RelaunchAsAdministrator();
                        }
                        */
                    }
                }
            }
        }



        async Task LoadGamesAndDlls()
        {
            if (_loadingGamesAndDlls)
                return;

            _loadingGamesAndDlls = true;

            // TODO: Fade?
            LoadingStackPanel.Visibility = Visibility.Visible;

            var tasks = new List<Task>();
            tasks.Add(LoadGamesAsync());


            await Task.WhenAll(tasks);

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadingStackPanel.Visibility = Visibility.Collapsed;
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

            var dialog = new EasyContentDialog(XamlRoot)
            {
                Title = "Filter",
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = gameFilterControl,
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Settings.Instance.HideNonDLSSGames = gameFilterControl.IsHideNonDLSSGamesChecked();
                Settings.Instance.GroupGameLibrariesTogether = gameFilterControl.IsGroupGameLibrariesTogetherChecked();

                FilterGames();
            }
        }
    }
}
