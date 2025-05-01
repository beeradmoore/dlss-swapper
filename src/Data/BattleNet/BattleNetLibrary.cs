using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLSS_Swapper.Data.BattleNet.Proto;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.BattleNet;

internal partial class BattleNetLibrary : IGameLibrary
{
    private static readonly Lazy<BattleNetLibrary> _instance = new(() => new BattleNetLibrary());

    private readonly string _productDbPath;

    public static BattleNetLibrary Instance => _instance.Value;

    public GameLibrary GameLibrary => GameLibrary.BattleNet;
    public string Name => "Battle.net";
    public Type GameType => typeof(BattleNetGame);

    private BattleNetLibrary()
    {
        var allUsersProfile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
        allUsersProfile ??= Environment.ExpandEnvironmentVariables("%ProgramData%");
        _productDbPath = Path.Combine(allUsersProfile, "Battle.net", "Agent", "product.db");
    }

    public bool IsInstalled()
    {
        var agentPath = Directory.GetParent(_productDbPath)?.FullName;
        return Directory.Exists(agentPath) && File.Exists(_productDbPath);
    }

    public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing)
    {
        if (!IsInstalled())
        {
            return [];
        }

        var games = new List<Game>();
        var tempFile = Path.GetTempFileName();

        try
        {
            File.Copy(_productDbPath, tempFile, true);
            await using var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                FileOptions.DeleteOnClose);
            var productDb = ProductDb.Parser.ParseFrom(fs);
            foreach (var product in productDb.ProductInstalls)
            {
                var game = ProcessProductEntry(product);
                if (game is null)
                {
                    continue;
                }

                games.Add(game);
            }
        }
        catch (FileNotFoundException ex)
        {
            Logger.Error($"Battle.net product.db not found: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Logger.Error($"I/O Error while reading Battle.net product.db: {ex.Message}");
            throw;
        }

        games.Sort();

        //Process all games that need processing
        foreach (var game in games.Where(game => game.NeedsProcessing || forceNeedsProcessing))
        {
            game.ProcessGame();
        }

        var cachedGames = GameManager.Instance.GetGames<BattleNetGame>();
        // Delete games no longer loaded (likely uninstalled)
        foreach (var cachedGame in cachedGames.Where(cachedGame => !games.Contains(cachedGame)))
        {
            await cachedGame.DeleteAsync().ConfigureAwait(false);
        }

        return games;
    }

    // Ignore the Battle.net agent installation and all World of Warcraft installations.
    // WoW DLL swaps lead to disconnects without exception, and it only supports XeLL anyway.
    [GeneratedRegex(@"^(agent|battle\.net|wow.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex IgnoredGameIdRegex();

    private static BattleNetGame? ProcessProductEntry(ProductInstall product)
    {
        if (IgnoredGameIdRegex().IsMatch(product.Uid))
        {
            return null;
        }

        // Uninstalled games sometimes remain in the product.db
        if (!product.CachedProductState.BaseProductState.Installed)
        {
            return null;
        }

        var gameId = product.Uid;
        var gamePath = product.Settings.InstallPath;

        if (string.IsNullOrEmpty(gameId))
        {
            Logger.Error("Issue loading Battle.net Game, no gameId found.");
            return null;
        }

        if (string.IsNullOrEmpty(gamePath))
        {
            Logger.Error("Issue loading Battle.net Game, no gamePath found.");
            return null;
        }

        if (!Directory.Exists(gamePath))
        {
            Logger.Error("Issue loading Battle.net Game, installation directory does not exist.");
            return null;
        }

        var cachedGame = GameManager.Instance.GetGame<BattleNetGame>(gameId);
        var activeGame = cachedGame ?? new BattleNetGame(gameId);
        activeGame.Title = product.GetTitle();
        activeGame.InstallPath = PathHelpers.NormalizePath(gamePath);
        activeGame.StatePlayable = product.CachedProductState.BaseProductState.Playable;

        if (activeGame.IsInIgnoredPath())
        {
            return null;
        }

        if (cachedGame is null)
        {
            activeGame.NeedsProcessing = true;
        }

        return activeGame;
    }

    public async Task LoadGamesFromCacheAsync()
    {
        try
        {
            BattleNetGame[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<BattleNetGame>().ToArrayAsync().ConfigureAwait(false);
            }

            foreach (var game in games)
            {
                if (game.IsInIgnoredPath())
                {
                    continue;
                }

                await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Debugger.Break();
        }
    }
}
