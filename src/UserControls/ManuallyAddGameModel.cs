using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data.CustomDirectory;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Gaming.Input;
using Windows.Storage.Pickers;
using DLSS_Swapper.Data;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : ObservableObject
    {
        ManuallyAddedGame game;
        public ManuallyAddedGame Game => game;

        WeakReference<ManuallyAddGameControl> manuallyAddGameControlWeakReference = null;

        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath)
        {
            manuallyAddGameControlWeakReference = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);

            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = Path.GetFileName(installPath),
                InstallPath = installPath,
            };
        }

        [RelayCommand]
        async Task AddCoverImageAsync()
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
