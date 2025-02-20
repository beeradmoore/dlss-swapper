# How it works
This document is intended as a living document to explain the inner workings of DLSS Swapper. The intention is to understand what happens where and when without having to trace the code path.


## Data storage
For the installed version of DLSS Swapper it will store config files and caches in `%LOCALAPPDATA%\DLSS Swapper\`. The portable variant uses the path `StoredData\` relative to the main `DLSS Swapper.exe`.

We will refer to both of these as `StoragePath` going forwards.


## Application launch
The main thing `App.xaml.cs` does is setting up the SQLite database. This is stored at `StoragePath\dlss_swapper.db`. It was initially used as a game cache for improved app load times but as time went on other features were added such as managing favourites, notes, last used DLSS versions, etc. 

This will eventually load the main part of the application, the `GameGridPage`.


## GameGridPage
This page will call its `Page_Loaded` event where we call `InitialLoadAsync` on the `GameGridPageModel` object. This is called with `SafeFireAndForget`, but should probably be swapped to actual threads at some point. 

This calls `LoadGamesFromCacheAsync` on the `GameManager` object followed by `LoadGamesAsync` on the same object.

The intent is that `LoadGamesFromCacheAsync` is light and loads what is already known, `LoadGamesAsync`  follows up as a more thorough loading system for each of the enabled libraries adding/removing games where necessary.


### GameManager
The `GameManager` is a centralized object that is accessed by its `Instance` property. It is in charge of loading games, as well as setting up collections used by the UI. This way when something in the backing data changes it will be reflected automatically in the UI.

Working backwards in its constructor the main two things being bound to are the `GroupedGameCollectionViewSource` and `UngroupedGameCollectionViewSource` objects. These are accessed externally from the `GameGridPageModel` and its `ApplyGameGroupFilter` method. This method will assign the `View` property of either of the `CollectionViewSource` objects to the `CurrentCollectionView` depending if we want to group the games to their libraries or to show all games together. The `CurrentCollectionView` is bound to the main `GridView` on the `GameGridPage`.

Coming back to the `GameManager`, both the `GroupedGameCollectionViewSource` and `UngroupedGameCollectionViewSource` are actually "Grouped". `UngroupedGameCollectionViewSource` groups are `Favourites` as well as `AllGames`, whereas `GroupedGameCollectionViewSource` have the groups of `Favourites` as well as individual game libraries.

Each of these "Groups" is backed by a `AdvancedCollectionView` which has a source of `AllGames`. Each of these `AdvancedCollectionView` have various other properties depending what they are used for.

- `Filter` is used on each of the games in `AllGames` to determine if it should show in this list or not (eg. If it is a favourite, if it belongs to a given game library, etc).
- `ObserveFilterProperty` is used for the favourites group only. If a `IsFavourite` property is changed on any game it will update this collection to add or remove a game.
- `SortDescriptions` is used to keep all games sorted alphabetically, regardless of where it is in the `AllGames` list.

This means when a game is added/removed to `AllGames` it will be reflected in these collections, and then reflected in the UI.

### GameManager - LoadGamesFromCacheAsync
This method will get a `GameLibrary` object for each of the known game library objects (eg. `SteamLibrary`, `GOGLibrary`, etc), and if it is enabled in the app settings it will call `LoadGamesFromCacheAsync` on that game library. This is added to a list so they can all be awaited.


#### GameLibrary - LoadGamesFromCacheAsync
The `LoadGamesFromCacheAsync` method is implemented in each class that implements the `IGameLibrary` interface. All implementations load their own variant of the `IGame` objects from the database and then call `AddGame` on the `GameManager` object.

```
// TODO: Store known DLSS path locations for each game.
// TODO: This should validate the DLSS path and version. If the path does not exists or the version does not match, do not call add game.
// TODO: If there are no changes we can add and use all found games instantly and don't require LoadGamesAsync to be called.
```

### GameManager - AddGame
Locks for thread safety, checks if the `Game` object is already present in `AllGames`. If it is we return that already existing game to prevent duplicate objects of the same game. If it does not we add it and then return that object.

When a game is added the filters and predicates of the `AdvancedCollectionView` do their job and will determine if the game is show to the already bound `GridView` on `GameGridPage`.


### GameManager - LoadGamesAsync 
test