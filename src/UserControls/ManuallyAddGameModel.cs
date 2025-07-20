using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Data.ManuallyAdded;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.UserControls;

internal partial class ManuallyAddGameModel : ObservableObject
{
    ManuallyAddedGame _game;
    public ManuallyAddedGame Game => _game;

    WeakReference<ManuallyAddGameControl> _manuallyAddGameControlWeakReference;

    public ManuallyAddGameModelTranslationProperties TranslationProperties { get; } = new ManuallyAddGameModelTranslationProperties();

    public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath) : base()
    {
        _manuallyAddGameControlWeakReference = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);

        _game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
        {
            Title = Path.GetFileName(installPath),
            InstallPath = PathHelpers.NormalizePath(installPath),
        };
    }

    [RelayCommand]
    async Task AddCoverImageAsync()
    {
        if (_game.CoverImage == _game.ExpectedCustomCoverImage)
        {
            await _game.PromptToRemoveCustomCover();
            return;
        }

        _game.PromptToBrowseCustomCover();
    }
}
