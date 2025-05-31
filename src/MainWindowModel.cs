using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Translations.Windows;
using Microsoft.UI.Xaml;

namespace DLSS_Swapper;

public partial class MainWindowModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string LoadingMessage { get; set; } = "Loading";

    [ObservableProperty]
    public partial Visibility AcknowledgementsVisibility { get; set; } = Visibility.Collapsed;

    public MainWindowTranslationPropertiesViewModel TranslationProperties { get; } = new MainWindowTranslationPropertiesViewModel();

    public MainWindowModel()
    {

    }
}
