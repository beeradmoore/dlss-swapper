using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using Serilog;
using SQLite;

namespace DLSS_Swapper.Data.GOG
{
    internal class GOGLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.GOG;
        public string Name => "GOG";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public Type GameType => typeof(GOGGame);

        static GOGLibrary? instance = null;
        public static GOGLibrary Instance => instance ??= new GOGLibrary();

        private GOGLibrary()
        {

        }

        public bool IsInstalled()
        {
            // We check for the registry key as offline installers will still make this, even if
            // the galaxy-2.0.db from GOG Galaxy is not found.
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var registryKey = hklm.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if (registryKey is not null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            IsInstalled();
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            if (IsInstalled() == false)
            {
                return new List<Game>();
            }

            var gogGames = new List<GOGGame>();

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var registryKey = hklm.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if (registryKey is null)
                    {
                        // Something bad happened.
                        return new List<Game>(); ;
                    }

                    // For each of the installed games, setup an initial GOG
                    foreach (var subkey in registryKey.GetSubKeyNames())
                    {
                        using (var gameKey = registryKey.OpenSubKey(subkey))
                        {
                            if (gameKey is null)
                            {
                                continue;
                            }

                            var gameId = gameKey.GetValue("gameID") as string;
                            var gameName = gameKey.GetValue("gameName") as string;
                            var gamePath = gameKey.GetValue("path") as string;

                            if (string.IsNullOrEmpty(gameId))
                            {
                                Logger.Error("Issue loading GOG Game, no gameId found.");
                                continue;
                            }


                            if (string.IsNullOrEmpty(gameName))
                            {
                                Logger.Error("Issue loading GOG Game, no gameName found.");
                                continue;
                            }


                            if (string.IsNullOrEmpty(gamePath))
                            {
                                Logger.Error("Issue loading GOG Game, no gamePath found.");
                                continue;
                            }


                            // If the entry is DLC we don't need to show it as an individual item.
                            var dependsOn = gameKey.GetValue("dependsOn") as string;
                            if (string.IsNullOrEmpty(dependsOn) == false)
                            {
                                continue;
                            }

                            var game = GameManager.Instance.GetGame<GOGGame>(gameId) ?? new GOGGame(gameId);
                            game.Title = gameName;
                            game.InstallPath = gamePath;

                            gogGames.Add(game);
                        }
                    }
                }
            }

            // No installed games found.
            if (gogGames.Count == 0)
            {
                return new List<Game>();
            }


            // Now that we have games we attempt to load covers for them.


            // If GOG Galaxy is installed we can get images from it.
            var storageFileLocation = GetStorageFileLocation();
            if (string.IsNullOrWhiteSpace(storageFileLocation) == false && File.Exists(storageFileLocation) == true)
            {
                //await Task.Delay(1);
                var db = new SQLiteAsyncConnection(storageFileLocation, SQLiteOpenFlags.ReadOnly);
                
                // Default resource type for verticalCover images is 3. We default to this, but we also add try load it incase it changes.
                var webCacheResourceTypeId = 3;
                var webCacheResourceType = (await db.QueryAsync<WebCacheResourceType>("SELECT * FROM WebCacheResourceTypes WHERE type=?", "verticalCover").ConfigureAwait(false)).FirstOrDefault();
                if (webCacheResourceType is not null)
                {
                    webCacheResourceTypeId = webCacheResourceType.Id;
                }

                // Default resource type for originalImages is 378. We default to this, but we also add try load it incase it changes.
                var gamePieceTypeId = 378;
                var gamePieceType = (await db.QueryAsync<GamePieceType>("SELECT * FROM GamePieceTypes WHERE type=?", "originalImages").ConfigureAwait(false)).FirstOrDefault();
                if (gamePieceType is not null)
                {
                    gamePieceTypeId = gamePieceType.Id;
                }

                var programDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                // var limitedDetails = await db.QueryAsync<LimitedDetail>("SELECT * FROM LimitedDetails").ConfigureAwait(false);
                // var installedBaseProducts = await db.QueryAsync<InstalledBaseProduct>("SELECT * FROM InstalledBaseProducts").ConfigureAwait(false);
                foreach (var gogGame in gogGames)
                {
                    try
                    {
                        /*
                        var installedBaseProduct = (await db.QueryAsync<InstalledBaseProduct>("SELECT * FRO< InstalledBaseProducts WHERE ProductId=? LIMIT 1", gogGame.Id).ConfigureAwait(false)).FirstOrDefault();
                        if (installedBaseProduct is null)
                        {
                            continue;
                        }
                        */

                        var limitedDetail = (await db.QueryAsync<LimitedDetail>("SELECT * FROM LimitedDetails WHERE ProductId=? LIMIT 1", gogGame.PlatformId).ConfigureAwait(false)).FirstOrDefault();
                        if (limitedDetail is null)
                        {
                            continue;
                        }

                        var localCoverImages = new List<string>();

                        var releaseKey = $"gog_{gogGame.PlatformId}";
                        var fallbackImage = limitedDetail.ImagesData?.Logo2x ?? string.Empty;
                        var gamePieces = (await db.QueryAsync<GamePiece>("SELECT * FROM GamePieces WHERE releaseKey=? AND gamePieceTypeId=?", releaseKey, gamePieceTypeId).ConfigureAwait(false));
                        if (gamePieces?.Any() == true)
                        {
                            foreach (var gamePiece in gamePieces)
                            {
                                var originalImages = gamePiece.GetValueAsOriginalImages();
                                if (string.IsNullOrEmpty(originalImages?.VerticalCover) == false)
                                {
                                    fallbackImage = originalImages.VerticalCover;
                                    break;
                                }
                            }
                        }

                        var webCaches = await db.QueryAsync<WebCache>("SELECT * FROM WebCache WHERE releaseKey=?", releaseKey).ConfigureAwait(false);
                        foreach (var webCache in webCaches)
                        {
                            var webCacheResource = (await db.QueryAsync<WebCacheResource>("SELECT * FROM WebCacheResources WHERE webCacheId=? AND webCacheResourceTypeId=? LIMIT 1", webCache.Id, webCacheResourceTypeId).ConfigureAwait(false)).FirstOrDefault();
                            if (webCacheResource is not null)
                            {
                                var localCoverImage = Path.Combine(programDataDirectory, "GOG.com", "Galaxy", "webcache", webCache.UserId.ToString(), "gog", gogGame.PlatformId.ToString(), webCacheResource.Filename);
                                if (File.Exists(localCoverImage))
                                {
                                    localCoverImages.Add(localCoverImage);
                                }
                            }
                        }

                        var productId = limitedDetail.ProductId.ToString();
                        var currentGame = gogGames.FirstOrDefault<GOGGame>(game => game.PlatformId == productId);
                        if (currentGame is not null)
                        {
                            currentGame.FallbackHeaderUrl = fallbackImage;
                            currentGame.PotentialLocalHeaders.Clear();
                            currentGame.PotentialLocalHeaders.AddRange(localCoverImages);
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.Error($"Could not load {gogGame.PlatformId} - {err.Message}");
                    }
                }

                await db.CloseAsync();
                db = null;
            }

            // Check for games that are installed locally, but not added to GOG Galaxy.
            foreach (var gogGame in gogGames)
            {
                if (string.IsNullOrEmpty(gogGame.FallbackHeaderUrl))
                {
                    var webcachePath = Path.Combine(gogGame.InstallPath, "webcache.zip");
                    if (File.Exists(webcachePath))
                    {
                        using (var zip = ZipFile.Open(webcachePath, ZipArchiveMode.Read))
                        {
                            var resourcesEntry = zip.GetEntry("resources.json");
                            if (resourcesEntry is null)
                            {
                                Logger.Error($"Unable to load resources.json for {gogGame.PlatformId}.");
                                continue;
                            }

                            using (var resourcesStream = resourcesEntry.Open())
                            {
                                var limitedDetailImages = JsonSerializer.Deserialize(resourcesStream, SourceGenerationContext.Default.ResourceImages);
                                if (limitedDetailImages is null)
                                {
                                    Logger.Error($"Unable to deserialize resources.json for {gogGame.PlatformId}.");
                                    continue;
                                }

                                if (string.IsNullOrEmpty(limitedDetailImages.Logo) == false)
                                {
                                    var url = $"https://images.gog.com/{limitedDetailImages.Logo}";
                                    url = url.Replace("glx_logo", "glx_vertical_cover");

                                    gogGame.FallbackHeaderUrl = url;
                                }
                            }

                        }
                    }
                    else
                    {
                        Logger.Error($"Unable to get covers through any methods for {gogGame.PlatformId}.");
                    }
                }

                await gogGame.SaveToDatabaseAsync();
                // Set thumbnails, check DLSS.
                gogGame.ProcessGame();
            }

            _loadedGames.AddRange(gogGames);
            _loadedDLSSGames.AddRange(gogGames.Where(g => g.HasDLSS == true));

            return new List<Game>(gogGames);
        }

        /// <summary>
        /// This file only exists if GOG Galaxy is installed.
        /// </summary>
        /// <returns>galaxy-2.0.db location, or empty string if not found.</returns>
        string GetStorageFileLocation()
        {
            var programDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var storageFileLocation = Path.Combine(programDataDirectory, "GOG.com", "Galaxy", "storage", "galaxy-2.0.db");
            if (File.Exists(storageFileLocation))
            {
                return storageFileLocation;
            }

            return string.Empty;
        }

        public async Task<List<Game>> LoadFromCacheAsync()
        {
            try
            {
                var games = await App.CurrentApp.Database.Table<GOGGame>().ToListAsync();
                return games.ToList<Game>();
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
            return new List<Game>();
        }

        public async Task LoadGamesAsync()
        {
            await Task.Delay(1);
        }

        public async Task LoadGamesFromCacheAsync()
        {
            try
            {
                var games = await App.CurrentApp.Database.Table<GOGGame>().ToArrayAsync().ConfigureAwait(false);
                foreach (var game in games)
                {
                    await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                    GameManager.Instance.AddGame(game);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}
