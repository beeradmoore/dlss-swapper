using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Data.ManuallyAdded;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : ObservableObject
    {
        private readonly ManuallyAddedGame game;
        public ManuallyAddedGame Game => game;

        private WeakReference<ManuallyAddGameControl> manuallyAddGameControlWeakReference;

        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath)
        {
            manuallyAddGameControlWeakReference = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);

            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = Path.GetFileName(installPath),
                InstallPath = PathHelpers.NormalizePath(installPath),
            };
        }

        [RelayCommand]
        private async Task AddCoverImageAsync()
        {
            if (game.CoverImage == game.ExpectedCustomCoverImage)
            {
                await game.PromptToRemoveCustomCover();
                return;
            }

            await game.PromptToBrowseCustomCover();
        }
    }
}
