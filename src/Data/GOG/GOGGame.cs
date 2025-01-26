using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.GOG
{
    internal class GOGGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.GOG;

        public List<string> PotentialLocalHeaders { get; } = new List<string>();
        public string FallbackHeaderUrl { get; set; } = string.Empty;

        public GOGGame()
        {

        }

        public GOGGame(string gameId)
        {
            PlatformId = gameId;
            SetID();
        }

        protected override async Task UpdateCacheImageAsync()
        {
            foreach (var potentialLocalHeader in PotentialLocalHeaders)
            {
                if (File.Exists(potentialLocalHeader))
                {
                    await ResizeCoverAsync(potentialLocalHeader).ConfigureAwait(false);
                    return;
                }
            }

            await DownloadCoverAsync(FallbackHeaderUrl).ConfigureAwait(false);
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            if (game is GOGGame gogGame)
            {
                //_localHeaderImages = xboxGame._localHeaderImages;
            }

            return didChange;
        }
    }
}
