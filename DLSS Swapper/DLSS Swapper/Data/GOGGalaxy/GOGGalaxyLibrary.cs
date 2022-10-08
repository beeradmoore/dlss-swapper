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
        public string Name => "GOG Galaxy";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled()
        {
            return GetStorageFileLocation() != null;
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            // If we don't detect a GOG Galaxy install path return an empty list.
            var storageFileLocation = GetStorageFileLocation();
            if (string.IsNullOrWhiteSpace(storageFileLocation))
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

                /*
                
                GamePieces
                releaseKey,gamePieceTypeId,userId,value
                gog_1207658927,378,46988767830580582,"{""background"":""https:\/\/images.gog.com\/2c40bb032307ae1c58fccca9075bec260b844b868175d5c80047e5e6e6e60313_glx_bg_top_padding_7.webp?namespace=gamesdb"",""squareIcon"":""https:\/\/images.gog.com\/3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_square_icon_v2.webp?namespace=gamesdb"",""verticalCover"":""https:\/\/images.gog.com\/3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_vertical_cover.webp?namespace=gamesdb""}"


                WebCache
                id,releaseKey,userId
                141,gog_1207658927,46988767830580582


                WebCacheResources
                webCacheId,webCacheResourceTypeId,filename
                141,2,3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_square_icon_v2.webp
                141,3,3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_vertical_cover.webp


                WebCacheResourceTypes
                id,type
                1,background
                2,squareIcon
                3,verticalCover


                C:\ProgramData\GOG.com\Galaxy\webcache\46988767830580582\gog\1207658927\3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_vertical_cover.webp

                */

                // HeaderImage listed here and in webcache folder are different.
                game.DetectDLSS();
                games.Add(game);
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

            return null;
        }
    }
}
