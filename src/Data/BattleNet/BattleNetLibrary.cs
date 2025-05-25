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

    // Definitis are from Lutris https://github.com/lutris/lutris/blob/master/lutris/util/battlenet/definitions.py
    private readonly Dictionary<string, string> _uidToTitles = new Dictionary<string, string>()
    {
        { "s1", "StarCraft" },
        { "s2", "StarCraft II" },
        { "wow", "World of Warcraft" },
        { "wow_classic", "World of Warcraft Classic" },
        { "pro", "Overwatch 2" },
        { "w2bn", "Warcraft II: Battle.net Edition" },
        { "w3", "Warcraft III" }, //
        { "hsb", "Hearthstone" },
        { "hero", "Heroes of the Storm" },
        { "d3cn", "暗黑破壞神III" },
        { "d3", "Diablo III" },
        { "fenris", "Diablo IV" },
        { "viper", "Call of Duty: Black Ops 4" },
        { "odin", "Call of Duty: Modern Warfare" },
        { "lazarus", "Call of Duty: MW2 Campaign Remastered" },
        { "zeus", "Call of Duty: Black Ops Cold War" },
        { "rtro", "Blizzard Arcade Collection" },
        { "wlby", "Crash Bandicoot 4: It's About Time" },
        { "osi", "Diablo® II: Resurrected" },
        { "fore", "Call of Duty: Vanguard" },
        { "d2", "Diablo® II" },
        { "d2LOD", "Diablo® II: Lord of Destruction®" },
        { "w3ROC", "Warcraft® III: Reign of Chaos" },
        { "w3tft", "Warcraft® III: The Frozen Throne®" },
        { "sca", "StarCraft® Anthology" },
        { "anbs", "Diablo Immortal" }
    };

    // Ignore the Battle.net agent installation and all World of Warcraft installations.
    // WoW DLL swaps lead to disconnects without exception, and it only supports XeLL anyway.
    [GeneratedRegex(@"^(agent|beta|battle\.net|wow.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex IgnoredGameIdRegex();


    private BattleNetLibrary()
    {
        var allUsersProfile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
        allUsersProfile ??= Environment.ExpandEnvironmentVariables("%ProgramData%");
        _productDbPath = Path.Combine(allUsersProfile, "Battle.net", "Agent", "product.db");
    }

    public bool IsInstalled()
    {
        // Registry checks if Battle.net client is installed, however the games can remain installed even if the client is uninstalled.
        /*
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
        */

        var agentPath = Directory.GetParent(_productDbPath)?.FullName;
        return Directory.Exists(agentPath) && File.Exists(_productDbPath);
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
                if (_uidToTitles.TryGetValue(product.Uid, out var title))
                {
                    activeGame.Title = title;
                }
                else
                {
                    Logger.Error($"Battle.Net game title not found for UID ({product.Uid}) in install path ({product.Settings.InstallPath}).");

                    if (string.IsNullOrWhiteSpace(product.Settings.InstallPath))
                    {
                        activeGame.Title = product.Uid;
                    }

                    var directoryInfo = new DirectoryInfo(product.Settings.InstallPath);
                    activeGame.Title = directoryInfo.Name;
                }

                activeGame.InstallPath = PathHelpers.NormalizePath(gamePath);
                activeGame.StatePlayable = product.CachedProductState.BaseProductState.Playable;

                if (activeGame.IsInIgnoredPath())
                {
                    continue;
                }

                if (Directory.Exists(activeGame.InstallPath) == false)
                {
                    Logger.Error($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
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

                if (Directory.Exists(game.InstallPath) == false)
                {
                    Logger.Error($"{Name} library could not load game {game.Title} ({game.PlatformId}) from cache because install path does not exist: {game.InstallPath}");
                    // We remove the list of known game assets, but not the game itself.
                    // Removing the game will remove its history, notes, and other data.
                    // We don't want to do this in case it is just a temporary issue.
                    await game.RemoveGameAssetsFromCacheAsync().ConfigureAwait(false);
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
