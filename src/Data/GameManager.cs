using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    //public ObservableCollection<Game> FavouriteGames { get; } = new ObservableCollection<Game>();
    public ObservableCollection<Game> AllGames { get; } = new ObservableCollection<Game>();

    Dictionary<GameLibrary, ObservableCollection<Game>> GamesByLibrary { get; } = new Dictionary<GameLibrary, ObservableCollection<Game>>();

    public CollectionViewSource GroupedGameCollectionViewSource { get; init; }
    public CollectionViewSource UngroupedGameCollectionViewSource { get; init; }


    object gameLock = new object();

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
                return game.HasDLSS;
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
                return game.IsFavourite && game.HasDLSS;
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
                return game.GameLibrary == library && game.HasDLSS;
            };
        }

        return (obj) =>
        {
            return ((Game)obj).GameLibrary == library;
        };
    }

    private GameManager()
    {
        //AllGames.CollectionChanged += AllGames_CollectionChanged;
        //AllGames.CollectionChanged += AllGames_CollectionChanged;
        FavouriteGamesView = new AdvancedCollectionView(AllGames, true);
        FavouriteGamesView.Filter = GetPredicateForFavouriteGames(Settings.Instance.HideNonDLSSGames);
        FavouriteGamesView.ObserveFilterProperty(nameof(Game.IsFavourite));
        FavouriteGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));

        AllGamesView = new AdvancedCollectionView(AllGames, true);
        AllGamesView.Filter = GetPredicateForAllGames(Settings.Instance.HideNonDLSSGames);
        //AllGamesView.ObserveFilterProperty(nameof(Game.IsFavourite));
        AllGamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Title), SortDirection.Ascending));
        

        allGamesGroup = new GameGroup(String.Empty, AllGamesView);
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

            var gameView = new AdvancedCollectionView(AllGames, true);
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

    private void AllGames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            //e.ne
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {

        }
    }

    public async Task LoadGamesFromCacheAsync()
    {
        var tasks = new Dictionary<GameLibrary, Task>();
        foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
            if (gameLibrary.IsEnabled)
            {
                tasks[gameLibraryEnum] = gameLibrary.LoadGamesFromCacheAsync();
            }
        }

        await Task.WhenAll(tasks.Values);
    }

    public async Task LoadGamesAsync()
    {
        await Task.Delay(1);
        return;

        var tasks = new Dictionary<GameLibrary, Task<List<Game>>>();
        foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
        {
            var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
            tasks[gameLibraryEnum] = gameLibrary.ListGamesAsync();
        }

        await Task.WhenAll(tasks.Values);

        foreach (var gameTasks in tasks)
        {
            if (gameTasks.Value.Result.Any())
            {
                //GamesByLibrary[gameTasks.Key] = new ObservableCollection<Game>(gameTasks.Value.Result);
                foreach (var game in gameTasks.Value.Result)
                {
                    AddGame(game);
                }
            }
            else
            {
                //GamesByLibrary[gameTasks.Key] = new ObservableCollection<Game>();
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

    public Game AddGame(Game game)
    {
        lock (gameLock)
        {
            if (AllGames.Contains(game) == true)
            {
                // This probably checks the game collection twice looking for the game.
                // We could do away with this, but in theory this if is never hit
                var firstGame = AllGames.First<Game>(x => x.Equals(game));
                Debugger.Break();
                return firstGame;
            }
            else
            {
                AllGames.Add(game);
                return game;
            }
        }
    }

    public void RemoveGame(Game game)
    {
        lock (gameLock)
        {
            if (game.IsFavourite)
            {
                //FavouriteGames.Remove(game);
            }

            AllGames.Remove(game);
            //GamesByLibrary[game.GameLibrary].Remove(game);
        }
    }


}
