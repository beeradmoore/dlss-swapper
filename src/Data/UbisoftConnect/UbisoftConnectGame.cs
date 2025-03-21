using System.IO;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    [Table("UbisoftConnectGame")]
    internal class UbisoftConnectGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.UbisoftConnect;

        [Column("local_header_image")]
        public string LocalHeaderImage { get; set; } = string.Empty;

        [Column("remote_header_image")]
        public string RemoteHeaderImage { get; set; } = string.Empty;

        public UbisoftConnectGame()
        {

        }

        public UbisoftConnectGame(string installId)
        {
            PlatformId = installId;
            SetID();
        }

        protected override async Task UpdateCacheImageAsync()
        {
            if (File.Exists(LocalHeaderImage))
            {
                using (var fileStream = File.Open(LocalHeaderImage, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await ResizeCoverAsync(fileStream).ConfigureAwait(false);
                }
                return;
            }

            await DownloadCoverAsync(RemoteHeaderImage).ConfigureAwait(false);
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            if (game is UbisoftConnectGame ubisoftConnectGame)
            {
                //_localHeaderImages = xboxGame._localHeaderImages;
            }

            return didChange;
        }

        public override bool IsReadyToPlay() => true;
    }
}
