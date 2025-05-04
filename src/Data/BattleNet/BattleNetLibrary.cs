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
using Microsoft.Win32;

namespace DLSS_Swapper.Data.BattleNet;

internal partial class BattleNetLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.BattleNet;
    public string Name => "Battle.net";

    public Type GameType => typeof(BattleNetGame);

    static BattleNetLibrary? instance = null;
    public static BattleNetLibrary Instance => instance ??= new BattleNetLibrary();

    GameLibrarySettings? _gameLibrarySettings;
    public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

    readonly string _productDbPath;


    // Ignore the Battle.net agent installation and all World of Warcraft installations.
    // WoW DLL swaps lead to disconnects without exception, and it only supports XeLL anyway.
    [GeneratedRegex(@"^(agent|battle\.net|wow.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex IgnoredGameIdRegex();


    private BattleNetLibrary()
    {
        var allUsersProfile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
        allUsersProfile ??= Environment.ExpandEnvironmentVariables("%ProgramData%");
        _productDbPath = Path.Combine(allUsersProfile, "Battle.net", "Agent", "product.db");
    }

    public bool IsInstalled()
    {
        using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        {
            using (var bnet = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net"))
            {
                if (bnet is null)
                {
                    return false;
                }

                var installPath = bnet.GetValue("InstallLocation")?.ToString();
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    return false;
                }

                var clientPath = Path.Combine(installPath, "Battle.net.exe");
                return File.Exists(clientPath) && File.Exists(_productDbPath);
            }
        }
    }

    public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing)
    {
        if (IsInstalled() == false)
        {
            return [];
        }

        var games = new List<Game>();
        var tempFile = Path.GetTempFileName();

        var cachedGames = GameManager.Instance.GetGames<BattleNetGame>();

        try
        {

            File.Copy(_productDbPath, tempFile, true);
            ProductDb? productDb;
            await using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose))
            {
                productDb = ProductDb.Parser.ParseFrom(fileStream);
            }

            if (productDb is null)
            {
                Logger.Error("Could not load product.db in Battle.net.");
                return [];
            }

            foreach (var product in productDb.ProductInstalls)
            {
                if (IgnoredGameIdRegex().IsMatch(product.Uid))
                {
                    continue;
                }

                // Uninstalled games sometimes remain in the product.db
                if (product.CachedProductState.BaseProductState.Installed == false)
                {
                    continue;
                }

                var gameId = product.Uid;
                var gamePath = product.Settings.InstallPath;

                if (string.IsNullOrWhiteSpace(gameId))
                {
                    Logger.Error("Issue loading Battle.net Game, no gameId found.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(gamePath))
                {
                    Logger.Error($"Issue loading Battle.net Game {gameId}, no gamePath found.");
                    continue;
                }

                if (Directory.Exists(gamePath) == false)
                {
                    Logger.Error($"Issue loading Battle.net Game {gameId}, installation directory {gamePath} does not exist.");
                    continue;
                }

                var cachedGame = GameManager.Instance.GetGame<BattleNetGame>(gameId);
                var activeGame = cachedGame ?? new BattleNetGame(gameId);
                activeGame.Title = product.GetTitle();
                activeGame.InstallPath = PathHelpers.NormalizePath(gamePath);
                activeGame.StatePlayable = product.CachedProductState.BaseProductState.Playable;

                if (activeGame.IsInIgnoredPath())
                {
                    continue;
                }

                await activeGame.SaveToDatabaseAsync().ConfigureAwait(false);

                if (cachedGame is null)
                {
                    activeGame.NeedsProcessing = true;
                }

                if (activeGame.NeedsProcessing == true || forceNeedsProcessing == true)
                {
                    activeGame.ProcessGame();
                }

                games.Add(activeGame);
            }
        }
        catch (FileNotFoundException err)
        {
            Logger.Error($"Battle.net product.db not found: {err.Message}");
            return [];
        }
        catch (IOException err)
        {
            Logger.Error($"I/O Error while reading Battle.net product.db: {err.Message}");
            return [];
        }

        games.Sort();

        // Delete games that are no longer loaded, they are likely uninstalled
        foreach (var cachedGame in cachedGames)
        {
            // Game is to be deleted.
            if (games.Contains(cachedGame) == false)
            {
                await cachedGame.DeleteAsync().ConfigureAwait(false);
            }
        }

        return games;
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
        catch (Exception err)
        {
            Logger.Error(err);
            Debugger.Break();
        }
    }
}
