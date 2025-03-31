using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.EpicGamesStore
{
    internal class EpicGamesStoreLibrary : GameLibraryBase<EpicGamesStoreGame>, IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.EpicGamesStore;
        public string Name => "Epic Games Store";

        public Type GameType => typeof(EpicGamesStoreGame);

        static EpicGamesStoreLibrary? instance = null;
        public static EpicGamesStoreLibrary Instance => instance ??= new EpicGamesStoreLibrary();

        private EpicGamesStoreLibrary()
        {

        }

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetEpicRootDirectory()) == false;
        }

        private async Task<Dictionary<string, CacheItem>> GetCachedDictionaryAsync(string epicRootDirectory)
        {
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
            return cacheItemsDictionary;
        }

        public async Task LoadGamesFromCacheAsync(IEnumerable<LogicalDriveState> drives) => await base.LoadGamesFromCacheAsync(drives);

        public async Task<List<Game>> ListGamesAsync(IEnumerable<LogicalDriveState> drives, bool forceNeedsProcessing = false)
        {
            var games = new List<Game>();
            var epicRootDirectory = GetEpicRootDirectory();

            // EGS can be installed and pass this check even if there are no games installed.
            if (string.IsNullOrWhiteSpace(epicRootDirectory) || Directory.Exists(epicRootDirectory) == false)
            {
                return games;
            }

            var cachedGames = GameManager.Instance.GetGames<EpicGamesStoreGame>();
            //cachedGames = cachedGames.Where(cg => !drives.Any(d => !d.IsEnabled && cg.InstallPath.ToLower().StartsWith(d.DriveLetter.ToLower()))).ToList();

            var manifestsDirectory = Path.Combine(epicRootDirectory, "EpicGamesLauncher", "Data", "Manifests");
            if (Directory.Exists(manifestsDirectory) == false)
            {
                return games;
            }

            Dictionary<string, CacheItem> cacheItemsDictionary = await GetCachedDictionaryAsync(epicRootDirectory).ConfigureAwait(false);

            string[] foundManifestFiles = Directory.GetFiles(manifestsDirectory, "*.item");
            foreach (var manifestFile in foundManifestFiles)
            {
                try
                {
                    EpicGamesStoreGame? extractedGame = await ExtractGame(drives, manifestFile, cacheItemsDictionary).ConfigureAwait(false);

                    if (extractedGame is null)
                    {
                        continue;
                    }

                    await extractedGame.SaveToDatabaseAsync();

                    if (extractedGame.NeedsProcessing || forceNeedsProcessing)
                    {
                        extractedGame.ProcessGame();
                    }

                    games.Add(extractedGame);
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

        private async Task<EpicGamesStoreGame?> ExtractGame(IEnumerable<LogicalDriveState> drives, string manifestFile, Dictionary<string, CacheItem> cacheItemsDictionary)
        {
            var manifestJsonData = await File.ReadAllTextAsync(manifestFile).ConfigureAwait(false);
            var manifest = JsonSerializer.Deserialize(manifestJsonData, SourceGenerationContext.Default.ManifestFile);

            if (manifest is null)
            {
                return null;
            }

            // Check that it is a game.
            if (manifest?.AppCategories.Contains("games") != true)
            {
                return null;
            }

            //Checks if game location on allowed drive
            if (drives.Any(d => !d.IsEnabled && manifest.InstallLocation.ToLower().StartsWith(d.DriveLetter.ToLower())))
            {
                return null;
            }

            // Check that is is the base game
            if (manifest.AppName != manifest.MainGameAppName)
            {
                return null;
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
            activeGame.NeedsProcessing = cachedGame is null;
            return activeGame;
        }
    }
}
