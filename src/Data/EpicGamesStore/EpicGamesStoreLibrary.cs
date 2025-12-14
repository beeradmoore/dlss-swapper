using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.OnlineId;

namespace DLSS_Swapper.Data.EpicGamesStore
{
    internal class EpicGamesStoreLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.EpicGamesStore;
        public string Name => "Epic Games Store";

        public Type GameType => typeof(EpicGamesStoreGame);

        static EpicGamesStoreLibrary? instance = null;
        public static EpicGamesStoreLibrary Instance => instance ??= new EpicGamesStoreLibrary();

        GameLibrarySettings? _gameLibrarySettings;
        public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

        private EpicGamesStoreLibrary()
        {

        }

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetEpicRootDirectory()) == false;
        }

        public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing = false)
        {
            var games = new List<Game>();
            var epicRootDirectory = GetEpicRootDirectory();

            // EGS can be installed and pass this check even if there are no games installed.
            if (string.IsNullOrWhiteSpace(epicRootDirectory) || Directory.Exists(epicRootDirectory) == false)
            {
                return games;
            }

            var cachedGames = GameManager.Instance.GetGames<EpicGamesStoreGame>();

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
                var cacheItemsArray = new CacheItem[0];
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
                    if (manifest.AppName != manifest.MainGameAppName)
                    {
                        continue;
                    }

                    var remoteHeaderUrl = string.Empty;
                    if (cacheItemsDictionary.ContainsKey(manifest.CatalogItemId))
                    {
                        var cacheItem = cacheItemsDictionary[manifest.CatalogItemId];
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
                    activeGame.RemoteHeaderImage = remoteHeaderUrl;
                    activeGame.Title = manifest.DisplayName; // TODO: Will this be a problem if the game is already loaded
                    activeGame.InstallPath = PathHelpers.NormalizePath(manifest.InstallLocation);

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

        string GetEpicRootDirectory()
        {
            var epicRootDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramData%"), "Epic");
            if (Directory.Exists(epicRootDirectory))
            {
                return epicRootDirectory;
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
}
