using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using FuzzySharp;

namespace DLSS_Swapper.Data.EAApp;

internal class EAAppLibrary : IGameLibrary
{
    static EAAppLibrary? instance;
    public static EAAppLibrary Instance => instance ??= new EAAppLibrary();

    public GameLibrary GameLibrary => GameLibrary.EAApp;

    GameLibrarySettings? _gameLibrarySettings;
    public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

    public string Name => "EA App";

    public Type GameType => typeof(EAAppGame);

    readonly FrozenSet<GameSearchResult> _gameSearchResults = [];

    private EAAppLibrary()
    {
        // The best way to get covers from EA apps is a static list from the EAAppGameListBuilder tool.
        try
        {
            var eaAppTitlesJsonPath = @"Assets\ea_app_titles.json";
            if (File.Exists(eaAppTitlesJsonPath) == true)
            {
                using (var fileStream = File.OpenRead(eaAppTitlesJsonPath))
                {
                    var gameSearchResults = JsonSerializer.Deserialize<List<GameSearchResult>>(fileStream);
                    if (gameSearchResults is null || gameSearchResults.Count == 0)
                    {
                        throw new Exception($"{eaAppTitlesJsonPath} is empty or invalid.");
                    }
                    _gameSearchResults = gameSearchResults.ToFrozenSet();
                }
            }
            else
            {
                throw new Exception($"{eaAppTitlesJsonPath} not found.");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Unable to load ea_app_titles.json.");
        }
    }

    public bool IsInstalled()
    {
        using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        {
            using (var eaDesktopKey = hklm.OpenSubKey(@"SOFTWARE\Electronic Arts\EA Desktop"))
            {
                if (eaDesktopKey is null)
                {
                    return false;
                }

                var installPath = eaDesktopKey.GetValue("InstallLocation")?.ToString();
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    return false;
                }

                if (Directory.Exists(installPath) == false)
                {
                    return false;
                }

                return true;
            }
        }
    }

    public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing)
    {
        if (IsInstalled() == false)
        {
            return new List<Game>();
        }

        var games = new List<Game>();
        var cachedGames = GameManager.Instance.GetGames<EAAppGame>();

        // I had no idea how to discover install EA App games until I had come across Flow.Launcher.Plugin.GamesLauncher
        // repo by KrystianLesniak.
        // https://github.com/KrystianLesniak/Flow.Launcher.Plugin.GamesLauncher
        // It works by looking at all installed applications and looking at the ones that have "EAInstaller" and "Cleanup.exe"
        // in the install path. Below is heavily based off their implementation.


        var registryHives = new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
        var registryViews = new RegistryView[] { RegistryView.Registry32, RegistryView.Registry64 };

        var uninstallRootKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";
        var gameListLock = new Lock();

        await Parallel.ForEachAsync(registryHives, async (hive, ct) =>
        {
            await Parallel.ForEachAsync(registryViews, async (view, ct) =>
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                {
                    using (var uninstallSubKey = baseKey.OpenSubKey(uninstallRootKey))
                    {
                        // Check if the uninstall sub key exists
                        if (uninstallSubKey is null)
                        {
                            return;
                        }

                        foreach (var programUninstallSubKeyName in uninstallSubKey.GetSubKeyNames())
                        {
                            var fullSubKey = $"{uninstallRootKey}{programUninstallSubKeyName}";
                            try
                            {
                                using (var programUninstallSubKey = baseKey.OpenSubKey(fullSubKey))
                                {
                                    if (programUninstallSubKey is null)
                                    {
                                        // Could not open program uninstall sub key
                                        continue;
                                    }

                                    var uninstallString = programUninstallSubKey.GetValue("UninstallString")?.ToString() ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(uninstallString))
                                    {
                                        // No uninstall string found
                                        continue;
                                    }

                                    if (uninstallString.Contains("EAInstaller", StringComparison.OrdinalIgnoreCase) == false ||
                                        uninstallString.Contains("Cleanup.exe", StringComparison.OrdinalIgnoreCase) == false)
                                    {
                                        continue;
                                    }


                                    var name = programUninstallSubKey.GetValue("DisplayName")?.ToString() ?? string.Empty;
                                    var installPath = programUninstallSubKey.GetValue("InstallLocation")?.ToString() ?? string.Empty;

                                    if (string.IsNullOrWhiteSpace(installPath))
                                    {
                                        Logger.Error($"Install path was empty for {name} in key {fullSubKey}");
                                        continue;
                                    }

                                    var installerDataPath = Path.Combine(installPath, "__Installer", "installerdata.xml");

                                    string contentId = string.Empty;
                                    if (File.Exists(installerDataPath))
                                    {
                                        var doc = XDocument.Load(installerDataPath);
                                        contentId = doc.Descendants("contentID").FirstOrDefault()?.Value ?? string.Empty;
                                    }

                                    if (string.IsNullOrWhiteSpace(contentId))
                                    {
                                        Logger.Error($"contentID was empty for {name} in key {fullSubKey}, from installer data {installerDataPath}");
                                        continue;
                                    }

                                    var cachedGame = GameManager.Instance.GetGame<EAAppGame>(contentId);
                                    var activeGame = cachedGame ?? new EAAppGame(contentId);

                                    activeGame.Title = name;
                                    activeGame.InstallPath = installPath;
                                    activeGame.DisplayIconPath = programUninstallSubKey.GetValue("DisplayIcon")?.ToString()?.Trim('"') ?? string.Empty;

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

                                    lock (gameListLock)
                                    {
                                        games.Add(activeGame);
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                Logger.Error(err, $"Could not prcoess key {fullSubKey}.");
                            }
                        }
                    }
                }
            });
        });

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
            EAAppGame[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<EAAppGame>().ToArrayAsync().ConfigureAwait(false);
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

    internal string SearchForCover(Game game)
    {
        // Use ExtractOne with a selector to match by the Name property
        var search = new GameSearchResult()
        {
            Title = game.Title,
        };
        var bestMatch = FuzzySharp.Process.ExtractOne(search, _gameSearchResults, g => g.Title);

        if (bestMatch is null || bestMatch.Score < 60)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(bestMatch.Value.PackArtImage?.Path) == false)
        {
            return bestMatch.Value.PackArtImage.Path;
        }

        if (string.IsNullOrWhiteSpace(bestMatch.Value.KeyArtImage?.Path) == false)
        {
            return bestMatch.Value.KeyArtImage.Path;
        }

        if (string.IsNullOrWhiteSpace(bestMatch.Value.LogoImage?.Path) == false)
        {
            return bestMatch.Value.LogoImage.Path;
        }

        return string.Empty;
    }
}
