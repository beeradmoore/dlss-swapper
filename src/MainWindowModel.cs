using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml;

namespace DLSS_Swapper;

public partial class MainWindowModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string LoadingMessage { get; set; } = ResourceHelper.GetString("Loading");

    [ObservableProperty]
    public partial Visibility AcknowledgementsVisibility { get; set; } = Visibility.Collapsed;

    public MainWindowModelTranslationProperties TranslationProperties { get; } = new MainWindowModelTranslationProperties();

    public MainWindowModel()
    {

    }
}
