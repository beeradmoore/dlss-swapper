using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    public string ClientPath { get; private set; } = string.Empty;
    string _installPath = string.Empty;

    // Definitions come from multiple places:
    // - https://github.com/dafzor/bnetlauncher/blob/master/bnetlauncher/Resources/gamesdb.ini
    // - https://github.com/lutris/lutris/blob/master/lutris/util/battlenet/definitions.py
    // - BattleNet agent log on launch
    //
    // Key is Uid, not ProductCode
    private readonly Dictionary<string, BattleNetLauncherGame> _knownGames = new Dictionary<string, BattleNetLauncherGame>()
    {
        { "rtro", new BattleNetLauncherGame("rtro", "rtro", "RTRO", "Blizzard Arcade Collection") },
        { "auks", new BattleNetLauncherGame("auks", "auks", "AUKS", "Call of Duty") },
        { "wlby", new BattleNetLauncherGame("wlby", "wlby", "WLBY", "Crash Bandicoot 4: It's About Time") },
        { "w1r", new BattleNetLauncherGame("w1r", "w1r", "W1R", "Warcraft I: Remastered") },
        { "diablo3", new BattleNetLauncherGame("diablo3", "d3", "D3", "Diablo III") },
        { "aris", new BattleNetLauncherGame("aris", "aris", "ARIS", "Doom: The Dark Ages") },
        { "heroes", new BattleNetLauncherGame("heroes", "hero", "Hero", "Heroes of the Storm") },
        { "d3cn", new BattleNetLauncherGame("d3cn", "d3cn", "D3CN", "暗黑破壞神III") }, // to verify
        { "aqua", new BattleNetLauncherGame("aqua", "aqua", "AQUA", "Avowed") },
        { "s2", new BattleNetLauncherGame("s2", "s2", "S2", "StarCraft II") },
        { "w2", new BattleNetLauncherGame("w2", "w2bn", "W2", "Warcraft II: Battle.net Edition") },
        { "fenris", new BattleNetLauncherGame("fenris", "fenris", "Fen", "Diablo IV") },
        { "d1", new BattleNetLauncherGame("d1", "drtl", "D1", "Diablo") },
        { "scor", new BattleNetLauncherGame("scor", "scor", "SCOR", "Sea of Thieves") },
        { "w3", new BattleNetLauncherGame("w3", "w3", "W3", "Warcraft III: Reforged") },
        { "fore", new BattleNetLauncherGame("fore", "fore", "FORE", "Call of Duty: Vanguard") }, // to verify
        { "s1", new BattleNetLauncherGame("s1", "s1", "S1", "StarCraft") },
        { "wow", new BattleNetLauncherGame("wow", "wow", "WoW", "World of Warcraft") },
        { "osi", new BattleNetLauncherGame("osi", "osi", "OSI", "Diablo II: Resurrected") },
        { "lazarus", new BattleNetLauncherGame("lazarus", "lazr", "LAZR", "Call of Duty: MW2 Campaign Remastered") }, // to verify
        { "odin", new BattleNetLauncherGame("odin", "odin", "ODIN", "Call of Duty: Modern Warfare") }, // to verify
        { "pinta", new BattleNetLauncherGame("pinta", "pinta", "PNTA", "Call of Duty: Modern Warfare III") }, // to verify
        { "prometheus", new BattleNetLauncherGame("prometheus", "pro", "Pro", "Overwatch") },
        { "viper", new BattleNetLauncherGame("viper", "viper", "VIPR", "Call of Duty: Black Ops 4") }, // to verify
        { "zeus", new BattleNetLauncherGame("zeus", "zeus", "ZEUS", "Call of Duty: Black Ops Cold War") }, // to verify
        { "w1", new BattleNetLauncherGame("w1", "war1", "W1", "Warcraft: Orcs & Humans") },
        { "w2r", new BattleNetLauncherGame("w2r", "w2r", "W2R", "Warcraft II Remastered") },
        { "hs_beta", new BattleNetLauncherGame("hs_beta", "hsb", "WTCG", "Hearthstone") },
        
        // Launcher isnt working, but that is ok as WoW is hidden.
        { "wow_classic", new BattleNetLauncherGame("wow_classic", "wow_classic", "Wow_wow_classic", "World of Warcraft Classic") }, // to verify

        // Does not appear in aggregate.json so they have no cover photos.
        { "lbra", new BattleNetLauncherGame("lbra", "lbra", "LBRA", "Tony Hawk's Pro Skater 3+4") },
        { "ark", new BattleNetLauncherGame("ark", "ark", "ARK", "The Outer Worlds 2") },
        { "nina", new BattleNetLauncherGame("nina", "nina", "NINA", "Call of Duty: Modern Warfare II") },


        // Phone games have no need to show up in app.
        //{ "anbs", new BattleNetLauncherGame("anbs", "anbs", "ANBS", "Diablo Immortal") },
        //{ "gryphon", new BattleNetLauncherGame("gryphon", "gryphon", "GRY", "Warcraft Rumble") },
        
        /*
        { "d2", "Diablo® II" },
        { "d2LOD", "Diablo® II: Lord of Destruction®" },
        { "w3ROC", "Warcraft® III: Reign of Chaos" },
        { "w3tft", "Warcraft® III: The Frozen Throne®" },
        { "sca", "StarCraft® Anthology" },
        */
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
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var bnet = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net"))
                {
                    if (bnet is not null)
                    {
                        var installPath = bnet.GetValue("InstallLocation")?.ToString();
                        if (string.IsNullOrWhiteSpace(installPath) == false)
                        {
                            var clientPath = Path.Combine(installPath, "Battle.net.exe");
                            if (File.Exists(clientPath))
                            {
                                _installPath = installPath;
                                ClientPath = clientPath;
                            }
                            else
                            {
                                Logger.Error($"Battle.net.exe not found at {clientPath}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Could not get BattleNet client path.");
        }

        var installedAggregates = new Dictionary<string, AggregateItem>();

        if (string.IsNullOrWhiteSpace(ClientPath) == false)
        {
            try
            {
                var aggregateJsonPath = Path.Combine(Directory.GetParent(_productDbPath)?.FullName ?? string.Empty, "aggregate.json");
                if (File.Exists(aggregateJsonPath) == true)
                {
                    using (var fileStream = File.OpenRead(aggregateJsonPath))
                    {
                        var aggregate = JsonSerializer.Deserialize<Aggregate>(fileStream);
                        if (aggregate is not null)
                        {
                            foreach (var aggregateItem in aggregate.Installed)
                            {
                                installedAggregates.Add(aggregateItem.ProductId, aggregateItem);
                            }
                        }
                        else
                        {
                            Logger.Error($"Could not deserialize aggregate {aggregateJsonPath}");
                        }
                    }
                }
                else
                {
                    Logger.Error($"Could not find {aggregateJsonPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load installed aggregates.");
            }
        }

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

                if (_knownGames.TryGetValue(product.Uid, out var battleNetLauncherGame))
                {
                    activeGame.Title = battleNetLauncherGame.Name;
                    activeGame.LauncherId = battleNetLauncherGame.LauncherId;
                }
                else
                {
                    Logger.Error($"Battle.Net game title not found for UID ({product.Uid}) in install path ({product.Settings.InstallPath}).");

                    if (string.IsNullOrWhiteSpace(product.Settings.InstallPath))
                    {
                        activeGame.Title = product.Uid;
                    }
                    else
                    {
                        var directoryInfo = new DirectoryInfo(product.Settings.InstallPath);
                        activeGame.Title = directoryInfo.Name;
                    }
                }

                if (installedAggregates.TryGetValue(product.ProductCode, out var aggregate))
                {
                    // If title isn't set, try use it from the aggregates.
                    if (string.IsNullOrWhiteSpace(activeGame.Title))
                    {
                        activeGame.Title = aggregate.Name;
                    }

                    // Set the cover photo.
                    activeGame.RemoteCoverImage = aggregate.LogoArtUri;
                }
                else
                {
                    Logger.Error($"Battle.Net game aggregate not found for ProductCode ({product.ProductCode}).");
                }

                activeGame.InstallPath = PathHelpers.NormalizePath(gamePath);
                activeGame.StatePlayable = product.CachedProductState.BaseProductState.Playable;

                if (activeGame.IsInIgnoredPath())
                {
                    continue;
                }

                if (Directory.Exists(activeGame.InstallPath) == false)
                {
                    Logger.Warning($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
                    continue;
                }

                await activeGame.SaveToDatabaseAsync().ConfigureAwait(false);

                if (cachedGame is null)
                {
                    activeGame.NeedsProcessing = true;
                }

                if (activeGame.NeedsProcessing == true || forceNeedsProcessing == true)
                {
                    activeGame.ProcessGame(forceNeedsProcessing: forceNeedsProcessing);
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
                    Logger.Warning($"{Name} library could not load game {game.Title} ({game.PlatformId}) from cache because install path does not exist: {game.InstallPath}");
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
