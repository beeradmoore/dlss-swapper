using DLSS_Swapper.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace DLSS_Swapper.Interfaces;
public abstract class GameLibraryBase<T> where T : Game, new()
{
    protected async Task LoadGamesFromCacheAsync(IEnumerable<LogicalDriveState> drives)
    {
        try
        {
            T[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<T>().ToArrayAsync().ConfigureAwait(false);
                if (drives.Any(d => !d.IsEnabled))
                {
                    games = games.Where(cg => !drives.Any(d => !d.IsEnabled && cg.InstallPath.ToLower().StartsWith(d.DriveLetter.ToLower()))).ToArray();
                }
            }
            foreach (var game in games)
            {
                await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            Debugger.Break();
        }
    } 
}
