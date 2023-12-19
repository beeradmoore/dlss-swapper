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
using Windows.Gaming.XboxLive.Storage;
using static DLSS_Swapper.Data.GOGGalaxy.LimitedDetail;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class GOGLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.GOG;
        public string Name => "GOG";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled()
        {
            // We check for the registry key as offline installers will still make this, even if
            // the galaxy-2.0.db from GOG Galaxy is not found.
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var registryKey = hklm.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if (registryKey != null)
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
                    if (registryKey == null)
                    {
                        // Something bad happened.
                        return new List<Game>(); ;
                    }

                    // For each of the installed games, setup an initial GOG
                    foreach (var subkey in registryKey.GetSubKeyNames())
                    {
                        using (var gameKey = registryKey.OpenSubKey(subkey))
                        {
                            if (gameKey == null)
                            {
                                continue;
                            }

                            var gameId = gameKey.GetValue("gameID") as String;
                            var gameName = gameKey.GetValue("gameName") as String;
                            var gamePath = gameKey.GetValue("path") as String;

                            if (Int32.TryParse(gameId, out int gameIdInt) == true && String.IsNullOrEmpty(gameName) == false && String.IsNullOrEmpty(gamePath) == false)
                            {
                                var game = new GOGGame()
                                {
                                    Id = gameIdInt,
                                    Title = gameName,
                                    InstallPath = gamePath,
                                };

                                game.DetectDLSS();

                                gogGames.Add(game);
                            }
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
            if (String.IsNullOrWhiteSpace(storageFileLocation) == false && File.Exists(storageFileLocation) == true)
            {
                //await Task.Delay(1);
                var db = new SQLiteAsyncConnection(storageFileLocation, SQLiteOpenFlags.ReadOnly);
              
                // Default resource type for verticalCover images is 3. We default to this, but we also add try load it incase it changes.
                var webCacheResourceTypeId = 3;
                var webCacheResourceType = (await db.QueryAsync<WebCacheResourceType>("SELECT * FROM WebCacheResourceTypes WHERE type=?", "verticalCover").ConfigureAwait(false)).FirstOrDefault();
                if (webCacheResourceType != null)
                {
                    webCacheResourceTypeId = webCacheResourceType.Id;
                }

                // Default resource type for originalImages is 378. We default to this, but we also add try load it incase it changes.
                var gamePieceTypeId = 378;
                var gamePieceType = (await db.QueryAsync<GamePieceType>("SELECT * FROM GamePieceTypes WHERE type=?", "originalImages").ConfigureAwait(false)).FirstOrDefault();
                if (gamePieceType != null)
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
                        if (installedBaseProduct == null)
                        {
                            continue;
                        }
                        */

                        var limitedDetail = (await db.QueryAsync<LimitedDetail>("SELECT * FROM LimitedDetails WHERE ProductId=? LIMIT 1", gogGame.Id).ConfigureAwait(false)).FirstOrDefault();
                        if (limitedDetail == null)
                        {
                            continue;
                        }

                        var localCoverImages = new List<string>();

                        var releaseKey = $"gog_{gogGame.Id}";
                        var fallbackImage = limitedDetail.ImagesData?.Logo2x ?? String.Empty;
                        var gamePieces = (await db.QueryAsync<GamePiece>("SELECT * FROM GamePieces WHERE releaseKey=? AND gamePieceTypeId=?", releaseKey, gamePieceTypeId).ConfigureAwait(false));
                        if (gamePieces?.Any() == true)
                        {
                            foreach (var gamePiece in gamePieces)
                            {
                                var originalImages = gamePiece.GetValueAsOriginalImages();
                                if (String.IsNullOrEmpty(originalImages.VerticalCover))
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
                            if (webCacheResource != null)
                            {
                                var localCoverImage = Path.Combine(programDataDirectory, "GOG.com", "Galaxy", "webcache", webCache.UserId.ToString(), "gog", gogGame.Id.ToString(), webCacheResource.Filename);
                                if (File.Exists(localCoverImage))
                                {
                                    localCoverImages.Add(localCoverImage);
                                }
                            }
                        }


                        var currentGame = gogGames.FirstOrDefault<GOGGame>(game => game.Id == limitedDetail.ProductId);
                        if (currentGame != null)
                        {
                            currentGame.FallbackHeaderUrl = fallbackImage;
                            currentGame.PotentialLocalHeaders.Clear();
                            currentGame.PotentialLocalHeaders.AddRange(localCoverImages);
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.Error($"Could not load {gogGame.Id} - {err.Message}");
                    }
                }

                await db.CloseAsync();
                db = null;
            }

            // Check for games that are installed locally, but not added to GOG Galaxy.
            foreach (var gogGame in gogGames)
            {
                if (String.IsNullOrEmpty(gogGame.FallbackHeaderUrl))
                {
                    var webcachePath = Path.Combine(gogGame.InstallPath, "webcache.zip");
                    if (File.Exists(webcachePath))
                    {
                        using (var zip = ZipFile.Open(webcachePath, ZipArchiveMode.Read))
                        {
                            var resourcesEntry = zip.GetEntry("resources.json");
                            if (resourcesEntry == null)
                            {
                                Logger.Error($"Unable to load resources.json for {gogGame.Id}.");
                                continue;
                            }

                            using (var resourcesStream = resourcesEntry.Open())
                            {
                                var limitedDetailImages = JsonSerializer.Deserialize<ResourceImages>(resourcesStream);
                                if (limitedDetailImages == null)
                                {
                                    Logger.Error($"Unable to deserialize resources.json for {gogGame.Id}.");
                                    continue;
                                }

                                if (String.IsNullOrEmpty(limitedDetailImages.Logo) == false)
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
                        Logger.Error($"Unable to get covers through any methods for {gogGame.Id}.");
                    }
                }
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

            return String.Empty;
        }
    }
}
