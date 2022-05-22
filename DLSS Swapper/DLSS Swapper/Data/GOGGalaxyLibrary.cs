using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Data.GOGGalaxy;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data
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
            return (GetStorageFileLocation() != null);
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
            var limitedDetails = db.Query<LimitedDetails>("SELECT * FROM LimitedDetails");
            var installedBaseProducts = db.Query<InstalledBaseProducts>("SELECT * FROM InstalledBaseProducts");
            db.Close();
            db = null;
            foreach (var limitedDetail in limitedDetails)
            {
                var installedBaseProduct = installedBaseProducts.FirstOrDefault(x => x.ProductId == limitedDetail.ProductId);
                if (installedBaseProduct == null)
                {
                    continue;
                }

                var game = new Game()
                {
                    Title = limitedDetail.Title,
                    InstallPath = installedBaseProduct.InstallationPath,
                    HeaderImage = limitedDetail.ImagesData?.Logo?.Replace("_logo.", "_vertical_cover."),
                };
                // HeaderImage listed here and in webcache folder are different.
                game.DetectDLSS();
                games.Add(game);
            }

            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));
            
            return games;
        }

        string? GetStorageFileLocation()
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
