using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Collections;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DLSS_Swapper.Data;

internal partial class GameManager : ObservableObject
{
    public static GameManager Instance { get; private set; } = new GameManager();

    // Because access to _allGames should be done on the UI thread we have _synchronisedAllGames which
    // will be used for adding/removing/fetching games. _allGames gets updated which will then be reflected
    // to the user.
    List<Game> _synchronisedAllGames = new List<Game>();
    ObservableCollection<Game> _allGames { get; } = new ObservableCollection<Game>();

    public CollectionViewSource GroupedGameCollectionViewSource { get; init; }
    public CollectionViewSource UngroupedGameCollectionViewSource { get; init; }

    [ObservableProperty]
    public partial bool UnknownAssetsFound { get; set; } = false;

    List<UnknownGameAsset> _unknownGameAssets { get; } = new List<UnknownGameAsset>();

    [ObservableProperty]
    public partial bool ShowHiddenGames { get; set; } = false;

    object gameLock = new object();
    object unknownGameAsseetLock = new object();

    GameGroup allGamesGroup;
    GameGroup favouriteGamesGroup;

    public AdvancedCollectionView AllGamesView { get; init; }
    public AdvancedCollectionView FavouriteGamesView { get; init; }

    Dictionary<GameLibrary, GameGroup> libraryGameGroups = new Dictionary<GameLibrary, GameGroup>();
    Dictionary<GameLibrary, AdvancedCollectionView> libraryGamesView = new Dictionary<GameLibrary, AdvancedCollectionView>();


    Predicate<object> GetPredicateForAllGames(bool hideNonDLSSGames, string? filterText = null)
    {
        return (obj) =>
        {
            var game = (Game)obj;

            if (ShowHiddenGames == false && game.IsHidden == true)
            {
                return false;
            }

            bool matchesText = string.IsNullOrEmpty(filterText) || game.Title.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return (!hideNonDLSSGames || game.HasSwappableItems) && matchesText;
        };
    }

    Predicate<object> GetPredicateForFavouriteGames(bool hideNonDLSSGames, string? filterText = null)
    {
        return (obj) =>
        {
            var game = (Game)obj;

            if (ShowHiddenGames == false && game.IsHidden == true)
            {
                return false;
            }

            bool matchesText = string.IsNullOrEmpty(filterText) || game.Title.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return game.IsFavourite && (!hideNonDLSSGames || game.HasSwappableItems) && matchesText;
        };
    }


    Predicate<object> GetPredicateForLibraryGames(GameLibrary library, bool hideNonDLSSGames, string? filterText = null)
    {
        return (obj) =>
        {
            var game = (Game)obj;

            if (ShowHiddenGames == false && game.IsHidden == true)
            {
                return false;
            }

            bool matchesText = string.IsNullOrEmpty(filterText) || game.Title.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return game.GameLibrary == library && (!hideNonDLSSGames || game.HasSwappableItems) && matchesText;
        };
    }

    private GameManager()
    {
        FavouriteGamesView = new AdvancedCollectionView(_allGames, true);
        FavouriteGamesView.Filter = GetPredicateForFavouriteGames(Settings.Instance.HideNonDLSSGames);
        FavouriteGamesView.ObserveFilterProperty(nameof(ShowHiddenGames));
        FavouriteGamesView.ObserveFilterProperty(nameof(Game.IsFavourite));
        FavouriteGamesView.ObserveFilterProperty(nameof(Game.HasSwappableItems));
        FavouriteGamesView.ObserveFilterProperty(nameof(Game.IsHidden));
        FavouriteGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));

        AllGamesView = new AdvancedCollectionView(_allGames, true);
        AllGamesView.Filter = GetPredicateForAllGames(Settings.Instance.HideNonDLSSGames);
        AllGamesView.ObserveFilterProperty(nameof(ShowHiddenGames));
        AllGamesView.ObserveFilterProperty(nameof(Game.HasSwappableItems));
        AllGamesView.ObserveFilterProperty(nameof(Game.IsHidden));
        AllGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));


        allGamesGroup = new GameGroup(string.Empty, null, AllGamesView);
        favouriteGamesGroup = new GameGroup("Favourites", null, FavouriteGamesView);

        var groupedList = new ObservableCollection<GameGroup>()
        {
            favouriteGamesGroup,
        };

        var ungroupedList = new List<GameGroup>()
        {
            favouriteGamesGroup,
            allGamesGroup,
        };


        foreach (var gameLibraryEnum in GetGameLibraries(false))
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);

            var gameView = new AdvancedCollectionView(_allGames, true);
            gameView.Filter = GetPredicateForLibraryGames(gameLibraryEnum, Settings.Instance.HideNonDLSSGames);
            gameView.ObserveFilterProperty(nameof(Game.HasSwappableItems));
            gameView.ObserveFilterProperty(nameof(ShowHiddenGames));
            gameView.ObserveFilterProperty(nameof(Game.IsHidden));
            gameView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));

            libraryGamesView[gameLibraryEnum] = gameView;

            var gameGroup = new GameGroup(gameLibrary.Name, gameLibrary.GameLibrary, gameView);
            groupedList.Add(gameGroup);
            libraryGameGroups[gameLibraryEnum] = gameGroup;
        }


        GroupedGameCollectionViewSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
            Source = groupedList,
            ItemsPath = new PropertyPath("Games"),
        };


        UngroupedGameCollectionViewSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
            Source = ungroupedList,
            ItemsPath = new PropertyPath("Games"),
        };


        WeakReferenceMessenger.Default.Register<GameLibrariesOrderChangedMessage>(this, (sender, message) =>
        {
            var groupedGameLibraryList = groupedList.ToList();

            groupedList.Clear();

            // Add favourites
            groupedList.Add(groupedGameLibraryList[0]);
            groupedGameLibraryList.RemoveAt(0);


            // Add each of the items in the order that is from settings.
            foreach (var gameLibrarySetting in Settings.Instance.GameLibrarySettings)
            {
                var groupedItem = groupedGameLibraryList.Single(x => x.GameLibrary == gameLibrarySetting.GameLibrary);
                groupedList.Add(groupedItem);
                groupedGameLibraryList.Remove(groupedItem);
            }

            if (groupedGameLibraryList.Count > 0)
            {
                Logger.Error($"Somehow extra grouped items were left over. {string.Join(", ", groupedGameLibraryList)}");
            }
        });

    }

    public async Task LoadGamesFromCacheAsync()
    {
        UnknownAssetsFound = false;
        _unknownGameAssets.Clear();

        foreach (var gameLibraryEnum in GameManager.Instance.GetGameLibraries(true))
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
            if (gameLibrary.IsEnabled)
            {
                await gameLibrary.LoadGamesFromCacheAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task LoadGamesAsync(bool forceNeedsProcessing = false)
    {
        var tasks = new List<Task<List<Game>>>();
        if (forceNeedsProcessing == true)
        {
            lock (unknownGameAsseetLock)
            {
                _unknownGameAssets.Clear();
            }
        }
        foreach (var gameLibraryEnum in GameManager.Instance.GetGameLibraries(true))
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
            if (gameLibrary.IsEnabled)
            {
                tasks.Add(gameLibrary.ListGamesAsync(forceNeedsProcessing));
            }
        }

        // Add games to the game library when the tasks is completed.
        while (tasks.Any())
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            foreach (var game in completedTask.Result)
            {
                AddGame(game);
            }
        }
    }

    public ICollectionView GetGameCollection(string? filterText = null)
    {
        // Refresh all filters.
        using (FavouriteGamesView.DeferRefresh())
        {
            FavouriteGamesView.Filter = GetPredicateForFavouriteGames(Settings.Instance.HideNonDLSSGames, filterText);
        }

        using (AllGamesView.DeferRefresh())
        {
            AllGamesView.Filter = GetPredicateForAllGames(Settings.Instance.HideNonDLSSGames, filterText);
        }

        if (Settings.Instance.GroupGameLibrariesTogether)
        {
            // Only refresh libraries when we are going to the grouped view.
            foreach (var keyValuePair in libraryGamesView)
            {
                using (keyValuePair.Value.DeferRefresh())
                {
                    keyValuePair.Value.Filter = GetPredicateForLibraryGames(keyValuePair.Key, Settings.Instance.HideNonDLSSGames, filterText);
                }
            }

            return GroupedGameCollectionViewSource.View;
        }
        else
        {
            return UngroupedGameCollectionViewSource.View;
        }
    }

    public List<Game> GetSynchronisedGamesListCopy()
    {
        lock (gameLock)
        {
            var list = new List<Game>(_synchronisedAllGames);
            return list;
        }
    }



    public Game AddGame(Game game, bool scrollIntoView = false)
    {
        lock (gameLock)
        {
            if (_synchronisedAllGames.Contains(game) == true)
            {
                // This probably checks the game collection twice looking for the game.
                // We could do away with this, but in theory this if is never hit
                var oldGame = _synchronisedAllGames.First(x => x.Equals(game));

                App.CurrentApp.RunOnUIThread(() =>
                {
                    oldGame.UpdateFromGame(game);
                });

                Debug.WriteLine($"Reusing old game: {game.Title}");
                return oldGame;
            }
            else
            {
                Debug.WriteLine($"Adding new game: {game.Title}");

                _synchronisedAllGames.Add(game);

                App.CurrentApp.RunOnUIThread(() =>
                {
                    _allGames.Add(game);

                    if (scrollIntoView)
                    {
                        App.CurrentApp.MainWindow.GameGridPage?.ScrollToGame(game);
                    }
                });

                return game;
            }
        }
    }

    public void RemoveGame(Game game)
    {
        lock (gameLock)
        {
            _synchronisedAllGames.Remove(game);

            App.CurrentApp.RunOnUIThread(() =>
            {
                _allGames.Remove(game);
            });
        }
    }

    public void RemoveAllGames()
    {
        lock (gameLock)
        {
            // TODO: Cancel loading of games here
            _synchronisedAllGames.Clear();

            App.CurrentApp.RunOnUIThread(() =>
            {
                _allGames.Clear();
            });
        }
    }

    public TGame? GetGame<TGame>(string platformId) where TGame : Game
    {
        lock (gameLock)
        {
            foreach (var game in _synchronisedAllGames)
            {
                if (game is TGame platformGame)
                {
                    if (game.PlatformId == platformId)
                    {
                        return platformGame;
                    }
                }
            }
        }

        return null;
    }

    public List<TGame> GetGames<TGame>() where TGame : Game
    {
        lock (gameLock)
        {
            var games = new List<TGame>();
            foreach (var game in _synchronisedAllGames)
            {
                if (game is TGame tGame)
                {
                    games.Add(tGame);
                }
            }
            return games;
        }
    }


    public bool CheckIfGameIsAdded(string installPath)
    {
        lock (gameLock)
        {
            foreach (var game in _synchronisedAllGames)
            {
                if (game.InstallPath?.Equals(installPath, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
        }
        return false;
    }


    public void AddUnknownGameAssets(GameLibrary gameLibrary, string gameTitle, List<GameAsset> gameAssets)
    {
        lock (unknownGameAsseetLock)
        {
            if (UnknownAssetsFound == false)
            {
                App.CurrentApp.RunOnUIThread(() =>
                {
                    UnknownAssetsFound = true;
                });
            }

            foreach (var gameAsset in gameAssets)
            {
                _unknownGameAssets.Add(new UnknownGameAsset(gameLibrary, gameTitle, gameAsset));
            }
        }
    }

    public List<UnknownGameAsset> GetUnknownGameAssets()
    {
        var unknownGameAssets = new List<UnknownGameAsset>();

        lock (unknownGameAsseetLock)
        {
            unknownGameAssets.AddRange(_unknownGameAssets);
        }

        return unknownGameAssets;
    }

    public GameLibrarySettings? GetGameLibrarySettings(GameLibrary gameLibrary)
    {
        return Settings.Instance.GameLibrarySettings.FirstOrDefault(x => x.GameLibrary == gameLibrary);
    }

    public List<GameLibrary> GetGameLibraries(bool onlyEnabled)
    {
        var gameLibrariesToReturn = new List<GameLibrary>();

        foreach (var gameLibrarySetting in Settings.Instance.GameLibrarySettings)
        {
            if (gameLibrarySetting.IsEnabled == false && onlyEnabled == true)
            {
                continue;
            }

            gameLibrariesToReturn.Add(gameLibrarySetting.GameLibrary);
        }

        return gameLibrariesToReturn;

    }
}
