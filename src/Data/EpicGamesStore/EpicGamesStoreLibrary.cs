using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.EpicGamesStore;

internal class EpicGamesStoreLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.EpicGamesStore;
    public string Name => "Epic Games Store";

    public Type GameType => typeof(EpicGamesStoreGame);

    static EpicGamesStoreLibrary? instance;
    public static EpicGamesStoreLibrary Instance => instance ??= new EpicGamesStoreLibrary();

    GameLibrarySettings? _gameLibrarySettings;
    public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

    private EpicGamesStoreLibrary()
    {

    }

    public bool IsInstalled()
    {
        if (string.IsNullOrEmpty(GetEpicRootDirectory()) == false)
        {
            return true;
        }

        var newLauncherRootDirectory = GetNewEpicLauncherRootDirectory();
        return string.IsNullOrEmpty(newLauncherRootDirectory) == false && File.Exists(Path.Combine(newLauncherRootDirectory, "installations.jsonl"));
    }

    public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing = false)
    {
        var games = new List<Game>();

        var cachedGames = GameManager.Instance.GetGames<EpicGamesStoreGame>();

        var legacyLauncherGames = await ListLegacyLauncherGamesAsync(forceNeedsProcessing).ConfigureAwait(false);
        games.AddRange(legacyLauncherGames);

        // The new (eglv2 / "beta") launcher uses a completely different data format
        // to the legacy launcher. If a game somehow shows up in both (e.g. during a
        // migration) we keep the legacy entry we have already added above.
        var newLauncherGames = await ListNewLauncherGamesAsync(forceNeedsProcessing).ConfigureAwait(false);
        foreach (var newLauncherGame in newLauncherGames)
        {
            if (games.Any(x => x.PlatformId == newLauncherGame.PlatformId) == false)
            {
                games.Add(newLauncherGame);
            }
        }

        // Delete games that are no longer loaded, they are likely uninstalled
        foreach (var cachedGame in cachedGames)
        {
            // Game is to be deleted.
            if (games.Contains(cachedGame) == false)
            {
                await cachedGame.DeleteAsync();
            }
        }

        return games;
    }

    // Handles the legacy Epic Games Launcher which stores manifests as individual
    // *.item files under %ProgramData%\Epic\EpicGamesLauncher\Data\Manifests.
    async Task<List<Game>> ListLegacyLauncherGamesAsync(bool forceNeedsProcessing)
    {
        var games = new List<Game>();
        var epicRootDirectory = GetEpicRootDirectory();

        // EGS can be installed and pass this check even if there are no games installed.
        if (string.IsNullOrWhiteSpace(epicRootDirectory) || Directory.Exists(epicRootDirectory) == false)
        {
            return games;
        }

        // Appears we may not need data from LauncherInstalled.dat if we just parse files in EpicGamesLauncher\Data\Manifests instead
        /*
        // Check the launcher installed file exists.
        var launcherInstalledFile = Path.Combine(epicRootDirectory, "UnrealEngineLauncher", "LauncherInstalled.dat");
        if (File.Exists(launcherInstalledFile) == false)
        {
            return games;
        }

        var launcherInstalledJsonData = await File.ReadAllTextAsync(launcherInstalledFile).ConfigureAwait(false);
        var launcherInstalledData = JsonSerializer.Deserialize<LauncherInstalled>(launcherInstalledJsonData);
        if (launcherInstalledData?.InstallationList?.Any() != true)
        {
            return games;
        }
        */

        var manifestsDirectory = Path.Combine(epicRootDirectory, "EpicGamesLauncher", "Data", "Manifests");
        if (Directory.Exists(manifestsDirectory) == false)
        {
            return games;
        }


        var cacheItemsDictionary = new Dictionary<string, CacheItem>();
        var catalogCacheFile = Path.Combine(epicRootDirectory, "EpicGamesLauncher", "Data", "Catalog", "catcache.bin");
        if (File.Exists(catalogCacheFile))
        {
            var cacheItemsArray = Array.Empty<CacheItem>();
            using (var fileStream = File.OpenRead(catalogCacheFile))
            {
                using (var memoryStream = new MemoryStream((int)fileStream.Length))
                {
                    await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                    var catalogCacheBase64 = Encoding.UTF8.GetString(memoryStream.ToArray());
                    var catalogCacheJson = Convert.FromBase64String(catalogCacheBase64);
                    cacheItemsArray = JsonSerializer.Deserialize(catalogCacheJson, SourceGenerationContext.Default.CacheItemArray);
                }
            }

            if (cacheItemsArray?.Any() == true)
            {
                foreach (var cacheItem in cacheItemsArray)
                {
                    cacheItemsDictionary[cacheItem.Id] = cacheItem;
                }
            }
        }



        var foundManifestFiles = Directory.GetFiles(manifestsDirectory, "*.item");
        foreach (var manifestFile in foundManifestFiles)
        {
            try
            {
                var manifestJsonData = await File.ReadAllTextAsync(manifestFile).ConfigureAwait(false);
                var manifest = JsonSerializer.Deserialize(manifestJsonData, SourceGenerationContext.Default.ManifestFile);

                // Check that it is a game.
                if (manifest?.AppCategories.Contains("games") != true)
                {
                    continue;
                }

                // Check that is is the base game
                //if (string.IsNullOrWhiteSpace(manifest.MainGameAppName) == false)
                //{
                //    continue;
                //}

                var remoteHeaderUrl = string.Empty;
                if (cacheItemsDictionary.TryGetValue(manifest.CatalogItemId, out var cacheItem))
                {
                    if (cacheItem.KeyImages?.Any() == true)
                    {
                        // Try get desired image.
                        var dieselGameBoxTall = cacheItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBoxTall");
                        if (dieselGameBoxTall is not null && string.IsNullOrEmpty(dieselGameBoxTall.Url) == false)
                        {
                            remoteHeaderUrl = dieselGameBoxTall.Url;
                        }
                        else
                        {
                            // Then fallback image.
                            var dieselGameBox = cacheItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBox");
                            if (dieselGameBox is not null && string.IsNullOrEmpty(dieselGameBox.Url) == false)
                            {
                                remoteHeaderUrl = dieselGameBox.Url;
                            }
                        }
                    }
                }


                var cachedGame = GameManager.Instance.GetGame<EpicGamesStoreGame>(manifest.CatalogItemId);
                var activeGame = cachedGame ?? new EpicGamesStoreGame(manifest.CatalogItemId);

                // These are UI-bound properties. The File.ReadAllTextAsync().ConfigureAwait(false) above has already
                // taken us off the UI thread, so these must be marshalled explicitly or WinUI throws RPC_E_WRONGTHREAD
                // when a bound control is updated from a background thread.
                await App.CurrentApp.RunOnUIThreadAsync(() =>
                {
                    activeGame.RemoteHeaderImage = remoteHeaderUrl;
                    activeGame.Title = manifest.DisplayName;
                    activeGame.InstallPath = PathHelpers.NormalizePath(manifest.InstallLocation);

                    return Task.CompletedTask;
                }).ConfigureAwait(false);

                if (activeGame.IsInIgnoredPath())
                {
                    continue;
                }

                if (Directory.Exists(activeGame.InstallPath) == false)
                {
                    Logger.Warning($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
                    continue;
                }

                await activeGame.SaveToDatabaseAsync();

                // If the game is not from cache, force processing
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
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        return games;
    }

    // Handles the new (eglv2 / "beta") Epic Games Launcher which stores its installed
    // games as an append-only installations.jsonl file (one JSON object per line) under
    // %LOCALAPPDATA%\com.epicgames.eglv2 instead of individual *.item manifest files.
    async Task<List<Game>> ListNewLauncherGamesAsync(bool forceNeedsProcessing)
    {
        var games = new List<Game>();

        var newLauncherRootDirectory = GetNewEpicLauncherRootDirectory();
        if (string.IsNullOrWhiteSpace(newLauncherRootDirectory))
        {
            return games;
        }

        var installationsFile = Path.Combine(newLauncherRootDirectory, "installations.jsonl");
        if (File.Exists(installationsFile) == false)
        {
            return games;
        }

        // Titles and box art are not stored in installations.jsonl, so we try to pull
        // them from the launcher's catalog cache. If this is missing or can't be
        // parsed we still show the game, just without a title or cover image.
        var catalogItemsDictionary = new Dictionary<string, CacheItem>();
        var catalogCacheFile = Path.Combine(newLauncherRootDirectory, "cache", "prod", "catalog-cache.json");
        if (File.Exists(catalogCacheFile))
        {
            try
            {
                var catalogCacheJsonData = await File.ReadAllTextAsync(catalogCacheFile).ConfigureAwait(false);
                var catalogCache = JsonSerializer.Deserialize(catalogCacheJsonData, SourceGenerationContext.Default.LauncherCatalogCache);
                if (catalogCache?.Items is not null)
                {
                    foreach (var (itemId, cacheEntry) in catalogCache.Items)
                    {
                        if (cacheEntry.Item is not null)
                        {
                            catalogItemsDictionary[itemId] = cacheEntry.Item;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        // installations.jsonl is an append-only log, so we keep only the most recent
        // line for each installation id to get its current state.
        var installationsByIdDictionary = new Dictionary<string, InstallationEntry>();
        var installationLines = await File.ReadAllLinesAsync(installationsFile).ConfigureAwait(false);
        foreach (var line in installationLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var installationEntry = JsonSerializer.Deserialize(line, SourceGenerationContext.Default.InstallationEntry);
                if (installationEntry is not null && string.IsNullOrEmpty(installationEntry.InstallationId) == false)
                {
                    installationsByIdDictionary[installationEntry.InstallationId] = installationEntry;
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        foreach (var installation in installationsByIdDictionary.Values)
        {
            try
            {
                // Sub-components/DLC installed alongside a base game, not a standalone game.
                if (string.IsNullOrEmpty(installation.ParentInstallationId) == false)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(installation.Dir) || string.IsNullOrWhiteSpace(installation.ItemId))
                {
                    continue;
                }

                catalogItemsDictionary.TryGetValue(installation.ItemId, out var catalogItem);

                // Check that it is a game, if we have category data to check it against.
                if (catalogItem?.Categories?.Any() == true && catalogItem.Categories.Any(x => x.Path == "games") == false)
                {
                    continue;
                }

                var remoteHeaderUrl = string.Empty;
                if (catalogItem?.KeyImages?.Any() == true)
                {
                    // Try get desired image.
                    var dieselGameBoxTall = catalogItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBoxTall");
                    if (dieselGameBoxTall is not null && string.IsNullOrEmpty(dieselGameBoxTall.Url) == false)
                    {
                        remoteHeaderUrl = dieselGameBoxTall.Url;
                    }
                    else
                    {
                        // Then fallback image.
                        var dieselGameBox = catalogItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBox");
                        if (dieselGameBox is not null && string.IsNullOrEmpty(dieselGameBox.Url) == false)
                        {
                            remoteHeaderUrl = dieselGameBox.Url;
                        }
                    }
                }

                var title = catalogItem?.Title ?? string.Empty;
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = Path.GetFileName(installation.Dir.TrimEnd('\\', '/'));
                }

                var cachedGame = GameManager.Instance.GetGame<EpicGamesStoreGame>(installation.ItemId);
                var activeGame = cachedGame ?? new EpicGamesStoreGame(installation.ItemId);

                // These are UI-bound properties. The File.ReadAllLinesAsync().ConfigureAwait(false) above has already
                // taken us off the UI thread, so these must be marshalled explicitly or WinUI throws RPC_E_WRONGTHREAD
                // when a bound control is updated from a background thread.
                await App.CurrentApp.RunOnUIThreadAsync(() =>
                {
                    activeGame.RemoteHeaderImage = remoteHeaderUrl;
                    activeGame.Title = title;
                    activeGame.InstallPath = PathHelpers.NormalizePath(installation.Dir);

                    return Task.CompletedTask;
                }).ConfigureAwait(false);

                if (activeGame.IsInIgnoredPath())
                {
                    continue;
                }

                if (Directory.Exists(activeGame.InstallPath) == false)
                {
                    Logger.Warning($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
                    continue;
                }

                await activeGame.SaveToDatabaseAsync();

                // If the game is not from cache, force processing
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
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        return games;
    }

    string GetEpicRootDirectory()
    {
        var epicRootDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramData%"), "Epic");
        if (Directory.Exists(epicRootDirectory))
        {
            return epicRootDirectory;
        }

        return string.Empty;
    }

    string GetNewEpicLauncherRootDirectory()
    {
        var newLauncherRootDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "com.epicgames.eglv2");
        if (Directory.Exists(newLauncherRootDirectory))
        {
            return newLauncherRootDirectory;
        }

        return string.Empty;
    }

    public async Task LoadGamesFromCacheAsync()
    {
        try
        {
            EpicGamesStoreGame[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<EpicGamesStoreGame>().ToArrayAsync().ConfigureAwait(false);
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
