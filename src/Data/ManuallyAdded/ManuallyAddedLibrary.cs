using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.CustomDirectory;

public class ManuallyAddedLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.ManuallyAdded;
    public string Name => "Manually Added";

    public List<Game> LoadedGames { get; } = new List<Game>();

    public List<Game> LoadedDLSSGames { get; } = new List<Game>();

    public Type GameType => typeof(ManuallyAddedGame);


    static ManuallyAddedLibrary? instance = null;
    public static ManuallyAddedLibrary Instance => instance ??= new ManuallyAddedLibrary();

    private ManuallyAddedLibrary()
    {

    }

    public async Task<List<Game>> ListGamesAsync(bool forceLoadAll = false)
    {
        LoadedGames.Clear();
        LoadedDLSSGames.Clear();

        // Not monitoring for cachedGames like in other libraries, as every game is from cache anyway.

        var games = new List<Game>();
        List<ManuallyAddedGame> dbGames;
        using (await Database.Instance.Mutex.LockAsync())
        {
            dbGames = await Database.Instance.Connection.Table<ManuallyAddedGame>().ToListAsync().ConfigureAwait(false);
        }
        foreach (var dbGame in dbGames)
        {

            // TODO: Handle process game
            dbGame.ProcessGame();
            games.Add(dbGame);
        }

        games.Sort();
        LoadedGames.AddRange(games);
        LoadedDLSSGames.AddRange(games.Where(g => g.HasSwappableItems));
        
        return games;
    }

    public bool IsInstalled()
    {
        return true;
    }

    public async Task LoadGamesFromCacheAsync()
    {
        try
        {
            ManuallyAddedGame[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<ManuallyAddedGame>().ToArrayAsync().ConfigureAwait(false);
            }
            foreach (var game in games)
            {
                // TODO: Handle process game
                await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
        }
    }
}
