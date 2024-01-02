using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled()
        {
            return String.IsNullOrEmpty(GetEpicRootDirectory()) == false;
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            var games = new List<Game>();
            var epicRootDirectory = GetEpicRootDirectory();

            // EGS can be installed and pass this check even if there are no games installed.
            if (String.IsNullOrWhiteSpace(epicRootDirectory) || Directory.Exists(epicRootDirectory) == false)
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

                if (cacheItemsArray?.Any() != false)
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

                    var remoteHeaderUrl = String.Empty;
                    if (cacheItemsDictionary.ContainsKey(manifest.CatalogItemId))
                    {
                        var cacheItem = cacheItemsDictionary[manifest.CatalogItemId];
                        if (cacheItem.KeyImages?.Any() == true)
                        {
                            // Try get desired image.
                            var dieselGameBoxTall = cacheItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBoxTall");
                            if (dieselGameBoxTall != null && String.IsNullOrEmpty(dieselGameBoxTall.Url) == false)
                            {
                                remoteHeaderUrl = dieselGameBoxTall.Url;
                            }
                            else
                            {
                                // Then fallback image.
                                var dieselGameBox = cacheItem.KeyImages.FirstOrDefault(x => x.Type == "DieselGameBox");
                                if (dieselGameBox != null && String.IsNullOrEmpty(dieselGameBox.Url) == false)
                                {
                                    remoteHeaderUrl = dieselGameBox.Url;
                                }
                            }
                        }
                    }

                    var game = new EpicGamesStoreGame(manifest.CatalogItemId) //remoteHeaderUrl)
                    {
                        RemoteHeaderImage = remoteHeaderUrl,
                        Title = manifest.DisplayName,
                        InstallPath = manifest.InstallLocation,
                    };
                    game.ProcessGame();
                    games.Add(game);
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }
            }

            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

            return games;
        }

        string GetEpicRootDirectory()
        {
            var epicRootDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramData%"), "Epic");
            if (Directory.Exists(epicRootDirectory))
            {
                return epicRootDirectory;
            }

            return String.Empty;
        }
    }
}
