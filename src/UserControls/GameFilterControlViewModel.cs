using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.UserControls;
public partial class GameFilterControlViewModel : ObservableObject
{
    public GameFilterControlViewModelTranslationProperties TranslationProperties { get; } = new GameFilterControlViewModelTranslationProperties();

    public GameFilterControlViewModel()
    {

    }
}
