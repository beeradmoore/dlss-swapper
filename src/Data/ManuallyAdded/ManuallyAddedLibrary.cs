using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.ManuallyAdded;

public class ManuallyAddedLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.ManuallyAdded;
    public string Name => "Manually Added";

    public Type GameType => typeof(ManuallyAddedGame);


    static ManuallyAddedLibrary? instance = null;
    public static ManuallyAddedLibrary Instance => instance ??= new ManuallyAddedLibrary();

    private ManuallyAddedLibrary()
    {

    }

    public async Task<List<Game>> ListGamesAsync(bool forceLoadAll = false)
    {
        List<Game> games = new List<Game>();
        List<ManuallyAddedGame> dbGames;
        using (await Database.Instance.Mutex.LockAsync())
        {
            dbGames = await Database.Instance.Connection.Table<ManuallyAddedGame>().ToListAsync().ConfigureAwait(false);
        }
        foreach (var dbGame in dbGames)
        {
            var cachedGame = GameManager.Instance.GetGame<ManuallyAddedGame>(dbGame.PlatformId);
            var activeGame = cachedGame ?? dbGame;

            // If the game is not from cache, force re-processing
            if (cachedGame is not null)
            {
                activeGame.NeedsReload = true;
            }

            if (activeGame.NeedsReload == true || forceLoadAll == true)
            {
                activeGame.ProcessGame();
            }

            games.Add(activeGame);
        }

        games.Sort();

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
                await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
            Debugger.Break();
        }
    }
}
