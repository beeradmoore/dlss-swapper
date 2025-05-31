using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Translations.UserControls;

namespace DLSS_Swapper.UserControls;
public partial class GameFilterControlViewModel : ObservableObject
{
    public GameFilterControlViewModel()
    {

    }

    public GameFilterTranslationPropertiesViewModel TranslationProperties { get; } = new GameFilterTranslationPropertiesViewModel();
}
