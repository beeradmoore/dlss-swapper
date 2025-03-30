using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Translations.Windows;

namespace DLSS_Swapper;
public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        TranslationProperties = new MainWindowTranslationPropertiesViewModel();
    }

    [ObservableProperty]
    public partial MainWindowTranslationPropertiesViewModel TranslationProperties { get; private set; }
}
