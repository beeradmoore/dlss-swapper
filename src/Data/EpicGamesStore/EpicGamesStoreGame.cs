using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.EpicGamesStore
{
    [Table("EpicGamesStoreGame")]
    internal class EpicGamesStoreGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.EpicGamesStore;

        [Column("remote_header_image")]
        public string RemoteHeaderImage { get; set;  } = string.Empty;

        public EpicGamesStoreGame()
        {

        }

        public EpicGamesStoreGame(string catalogItemId)
        {
            PlatformId = catalogItemId;
            SetID();
        }

        protected override async Task UpdateCacheImageAsync()
        {
            if (string.IsNullOrEmpty(RemoteHeaderImage))
            {
                return;
            }
            
            // If the remote image doens't already have query arguments lets add some to load a smaller image.
            if (RemoteHeaderImage.Contains("?") == false)
            {
                RemoteHeaderImage = RemoteHeaderImage + "?w=600&h=900&resize=1";
            }

            await DownloadCoverAsync(RemoteHeaderImage).ConfigureAwait(false);
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            if (game is EpicGamesStoreGame epicGamesStoreGame)
            {
                //_localHeaderImages = xboxGame._localHeaderImages;
            }

            return didChange;
        }

    }
}
