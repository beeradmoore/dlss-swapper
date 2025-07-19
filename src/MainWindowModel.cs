using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml;

namespace DLSS_Swapper;

public partial class MainWindowModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string LoadingMessage { get; set; } = ResourceHelper.GetString("General_Loading");

    [ObservableProperty]
    public partial Visibility AcknowledgementsVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial FlowDirection AppFlowDirection { get; set; } = FlowDirection.LeftToRight;

    public MainWindowModelTranslationProperties TranslationProperties { get; } = new MainWindowModelTranslationProperties();

    public MainWindowModel()
    {
        // Initialize FlowDirection based on current language
        UpdateFlowDirection();
        
        // Subscribe to language changes
        LanguageManager.Instance.OnLanguageChanged += UpdateFlowDirection;
    }

    private void UpdateFlowDirection()
    {
        var currentLanguage = Settings.Instance.Language;
        AppFlowDirection = LanguageManager.IsRightToLeftLanguage(currentLanguage) 
            ? FlowDirection.RightToLeft 
            : FlowDirection.LeftToRight;
    }
}
