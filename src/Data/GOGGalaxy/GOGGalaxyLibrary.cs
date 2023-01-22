using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class GOGGalaxyLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.GoG;
        public string Name => "GOG Galaxy";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled()
        {
            return String.IsNullOrEmpty(GetStorageFileLocation()) == false;
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            // If we don't detect a GOG Galaxy install path return an empty list.
            var storageFileLocation = GetStorageFileLocation();
            if (String.IsNullOrWhiteSpace(storageFileLocation))
            {
                return new List<Game>();
            }

            await Task.Delay(1);


            var games = new List<Game>();

            var db = new SQLiteConnection(storageFileLocation, SQLiteOpenFlags.ReadOnly);
            var limitedDetails = db.Query<LimitedDetail>("SELECT * FROM LimitedDetails");
            var installedBaseProducts = db.Query<InstalledBaseProduct>("SELECT * FROM InstalledBaseProducts");
            
            // Default resource type for verticalCover images is 3. We default to this, but we also add try load it incase it changes.
            var webCacheResourceTypeId = 3;
            var webCacheResourceType = db.Query<WebCacheResourceType>("SELECT * FROM WebCacheResourceTypes WHERE type=?", "verticalCover").FirstOrDefault();
            if (webCacheResourceType != null)
            {
                webCacheResourceTypeId = webCacheResourceType.Id;
            }

            // Default resource type for originalImages is 378. We default to this, but we also add try load it incase it changes.
            var gamePieceTypeId = 378;
            var gamePieceType = db.Query<GamePieceType>("SELECT * FROM GamePieceTypes WHERE type=?", "originalImages").FirstOrDefault();
            if (gamePieceType != null)
            {
                gamePieceTypeId = gamePieceType.Id;
            }

            var programDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            foreach (var installedBaseProduct in installedBaseProducts)
            {
                try
                {
                    var limitedDetail = limitedDetails.FirstOrDefault(x => x.ProductId == installedBaseProduct.ProductId);
                    if (limitedDetail == null)
                    {
                        continue;
                    }

                    var localCoverImages = new List<string>();

                    var releaseKey = $"gog_{limitedDetail.ProductId}";
                    var fallbackImage = limitedDetail.ImagesData?.Logo2x ?? String.Empty;
                    var gamePieces = db.Query<GamePiece>("SELECT * FROM GamePieces WHERE releaseKey=? AND gamePieceTypeId=?", releaseKey, gamePieceTypeId);
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

                    var webCaches = db.Query<WebCache>("SELECT * FROM WebCache WHERE releaseKey=?", releaseKey);
                    foreach (var webCache in webCaches)
                    {
                        var webCacheResource = db.Query<WebCacheResource>("SELECT * FROM WebCacheResources WHERE webCacheId=? AND webCacheResourceTypeId=?", webCache.Id, webCacheResourceTypeId).FirstOrDefault();
                        if (webCacheResource != null)
                        {
                            localCoverImages.Add(Path.Combine(programDataDirectory, "GOG.com", "Galaxy", "webcache", webCache.UserId.ToString(), "gog", limitedDetail.ProductId.ToString(), webCacheResource.Filename));
                        }
                    }


                    var game = new GOGGame(localCoverImages, fallbackImage)
                    {
                        Title = limitedDetail.Title,
                        InstallPath = installedBaseProduct.InstallationPath,
                        //HeaderImage = limitedDetail.ImagesData?.Logo?.Replace("_logo.", "_vertical_cover."),
                    };


                    game.DetectDLSS();
                    games.Add(game);
                }
                catch (Exception err)
                {
                    Logger.Error($"Could not load {installedBaseProduct.ProductId} - {err.Message}");
                }
            }

            db.Close();
            db = null;

            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

            return games;
        }

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
