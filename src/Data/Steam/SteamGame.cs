using System.IO;
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

        protected override async Task UpdateCacheImageAsync()
        {
            // Try get image from the local disk first.
            var localHeaderImagePath = Path.Combine(SteamLibrary.GetInstallPath(), "appcache", "librarycache", $"{PlatformId}_library_600x900.jpg");
            if (File.Exists(localHeaderImagePath))
            {
                await ResizeCoverAsync(localHeaderImagePath).ConfigureAwait(false);
                return;
            }

            // If it doesn't exist, load from web.
            await DownloadCoverAsync($"https://steamcdn-a.akamaihd.net/steam/apps/{PlatformId}/library_600x900_2x.jpg").ConfigureAwait(false);
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            return didChange;
        }
    }
}
