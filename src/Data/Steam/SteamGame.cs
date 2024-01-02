using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Steam
{
    [Table("SteamGame")]
    internal class SteamGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.Steam;

        public SteamGame()
        {

        }

        public SteamGame(string appId)
        {
            PlatformId = appId;
            SetID();
        }

        protected override void UpdateCacheImage()
        {
            // Try get image from the local disk first.
            var localHeaderImagePath = Path.Combine(SteamLibrary.GetInstallPath(), "appcache", "librarycache", $"{PlatformId}_library_600x900.jpg");
            if (File.Exists(localHeaderImagePath))
            {
                ResizeCover(localHeaderImagePath);
                return;
            }

            // If it doesn't exist, load from web.
            DownloadCover($"https://steamcdn-a.akamaihd.net/steam/apps/{PlatformId}/library_600x900_2x.jpg");
        }
    }
}
