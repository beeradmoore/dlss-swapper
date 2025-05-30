using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Steam
{
    [Table("SteamGame")]
    internal partial class SteamGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.Steam;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsReadyToPlay))]
        [Column("state_flags")]
        public partial SteamStateFlag StateFlags { get; set; }

        public override bool IsReadyToPlay
        {
            get
            {
                const SteamStateFlag allowedFlags = SteamStateFlag.StateFullyInstalled | SteamStateFlag.StateAppRunning;
                return StateFlags != 0 && (StateFlags & ~allowedFlags) == 0;
            }
        }

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
                using (var fileStream = File.Open(localHeaderImagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await ResizeCoverAsync(fileStream).ConfigureAwait(false);
                }
                return;
            }

            // If it doesn't exist, load from web.
            await DownloadCoverAsync($"https://steamcdn-a.akamaihd.net/steam/apps/{PlatformId}/library_600x900_2x.jpg").ConfigureAwait(false);
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            if (game is SteamGame steamGame)
            {
                if (StateFlags != steamGame.StateFlags)
                {
                    StateFlags = steamGame.StateFlags;
                    didChange = true;
                }
            }

            return didChange;
        }
    }
}
