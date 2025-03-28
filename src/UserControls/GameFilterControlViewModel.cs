using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Translations.UserControls;

namespace DLSS_Swapper.UserControls;
public partial class GameFilterControlViewModel : ObservableObject
{
    public GameFilterControlViewModel()
    {
        TranslationProperties = new GameFilterTranslationPropertiesViewModel();
    }

    [ObservableProperty]
    public partial GameFilterTranslationPropertiesViewModel TranslationProperties { get; private set; }
}
