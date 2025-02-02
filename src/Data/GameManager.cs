using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using DLSS_Swapper.Interfaces;
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

    object gameLock = new object();
    object unknownGameAsseetLock = new object();

    GameGroup allGamesGroup;
    GameGroup favouriteGamesGroup;

    public AdvancedCollectionView AllGamesView { get; init; }
    public AdvancedCollectionView FavouriteGamesView { get; init; }


    bool Filter_Favourite_WithDLSS(object obj)
    {
        return ((Game)obj).IsFavourite;
    }

    bool Filter_Favourite_Any(object obj)
    {
        return ((Game)obj).IsFavourite;
    }

    Dictionary<GameLibrary, GameGroup> libraryGameGroups = new Dictionary<GameLibrary, GameGroup>();
    Dictionary<GameLibrary, AdvancedCollectionView> libraryGamesView = new Dictionary<GameLibrary, AdvancedCollectionView>();


    Predicate<object> GetPredicateForAllGames(bool hideNonDLSSGames)
    {
        if (hideNonDLSSGames)
        {
            return (obj) =>
            {
                var game = (Game)obj;
                return game.HasSwappableItems;
            };
        }

        return (obj) =>
        {
            return true;
        };
    }

    Predicate<object> GetPredicateForFavouriteGames(bool hideNonDLSSGames)
    {
        if (hideNonDLSSGames)
        {
            return (obj) =>
            {
                var game = (Game)obj;
                return game.IsFavourite && game.HasSwappableItems;
            };
        }

        return (obj) =>
        {
            return ((Game)obj).IsFavourite;
        };
    }


    Predicate<object> GetPredicateForLibraryGames(GameLibrary library, bool hideNonDLSSGames)
    {
        if (hideNonDLSSGames)
        {
            return (obj) =>
            {
                var game = (Game)obj;
                return game.GameLibrary == library && game.HasSwappableItems;
            };
        }

        return (obj) =>
        {
            return ((Game)obj).GameLibrary == library;
        };
    }

    private GameManager()
    {
        FavouriteGamesView = new AdvancedCollectionView(_allGames, true);
        FavouriteGamesView.Filter = GetPredicateForFavouriteGames(Settings.Instance.HideNonDLSSGames);
        FavouriteGamesView.ObserveFilterProperty(nameof(Game.IsFavourite));
        FavouriteGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));

        AllGamesView = new AdvancedCollectionView(_allGames, true);
        AllGamesView.Filter = GetPredicateForAllGames(Settings.Instance.HideNonDLSSGames);
        //AllGamesView.ObserveFilterProperty(nameof(Game.IsFavourite));
        AllGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));


        allGamesGroup = new GameGroup(string.Empty, AllGamesView);
        favouriteGamesGroup = new GameGroup("Favourites", FavouriteGamesView);

        var groupedList = new List<GameGroup>()
        {
            favouriteGamesGroup,
        };

        var ungroupedList = new List<GameGroup>()
        {
            favouriteGamesGroup,
            allGamesGroup,
        };


        foreach (var gameLibraryEnum in Enum.GetValues<GameLibrary>())
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);

            var gameView = new AdvancedCollectionView(_allGames, true);
            gameView.Filter = GetPredicateForLibraryGames(gameLibraryEnum, Settings.Instance.HideNonDLSSGames);
            gameView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));

            libraryGamesView[gameLibraryEnum] = gameView;

            var gameGroup = new GameGroup(gameLibrary.Name, gameView);
            groupedList.Add(gameGroup);
            libraryGameGroups[gameLibraryEnum] = new GameGroup(gameLibrary.Name, gameView);
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

    }

    public async Task LoadGamesFromCacheAsync()
    {
        UnknownAssetsFound = false;
        _unknownGameAssets.Clear();

        foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
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
        foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
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

    public ICollectionView GetGameCollection()
    {
        // Refresh all filters.
        using (FavouriteGamesView.DeferRefresh())
        {
            FavouriteGamesView.Filter = GetPredicateForFavouriteGames(Settings.Instance.HideNonDLSSGames);
        }

        using (AllGamesView.DeferRefresh())
        {
            AllGamesView.Filter = GetPredicateForAllGames(Settings.Instance.HideNonDLSSGames);
        }


        if (Settings.Instance.GroupGameLibrariesTogether)
        {
            // Only refresh librarys when we are going to the grouped view.
            foreach (var keyValuePair in libraryGamesView)
            {
                using (keyValuePair.Value.DeferRefresh())
                {
                    keyValuePair.Value.Filter = GetPredicateForLibraryGames(keyValuePair.Key, Settings.Instance.HideNonDLSSGames);
                }
            }

            return GroupedGameCollectionViewSource.View;
        }
        else
        {
            return UngroupedGameCollectionViewSource.View;
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
}
