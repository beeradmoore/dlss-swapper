using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Xbox
{
    [Table("XboxGame")]
    public class XboxGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.XboxApp;

        List<string> _localHeaderImages = new List<string>();

        public XboxGame()
        {

        }

        public XboxGame(string familyName)
        {
            PlatformId = familyName;
            SetID();
        }

        internal async void SetLocalHeaderImagesAsync(List<string> localHeaderImages)
        {
            _localHeaderImages = localHeaderImages;
            await LoadCoverImageAsync();
        }

        protected override async Task UpdateCacheImageAsync()
        {
            foreach (var localHeaderImage in _localHeaderImages)
            {
                var headerImage = Path.Combine(InstallPath, localHeaderImage);
                if (File.Exists(headerImage))
                {
                    await ResizeCoverAsync(headerImage).ConfigureAwait(false);
                    return;
                }
            }
        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            return didChange;
        }
    }
}
