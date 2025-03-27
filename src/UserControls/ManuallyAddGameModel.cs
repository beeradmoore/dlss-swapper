using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Data.ManuallyAdded;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : LocalizedViewModelBase
    {
        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath) : base()
        {
            manuallyAddGameControlWeakReference = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);

            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = Path.GetFileName(installPath),
                InstallPath = PathHelpers.NormalizePath(installPath),
            };
        }

        ManuallyAddedGame game;
        public ManuallyAddedGame Game => game;

        WeakReference<ManuallyAddGameControl> manuallyAddGameControlWeakReference;

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

        #region LanguageProperties
        [LanguageProperty] public string AddCoverText => ResourceHelper.GetString("AddCover");
        [LanguageProperty] public string OptionalText => ResourceHelper.GetString("Optional");
        [LanguageProperty] public string NameText => ResourceHelper.GetString("Name");
        [LanguageProperty] public string InstallPathText => ResourceHelper.GetString("InstallPath");
        #endregion
    }
}
