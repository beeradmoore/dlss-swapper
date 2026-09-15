using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data.GitHub;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

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

    [ObservableProperty]
    public partial Visibility UpdateAvailableVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial string UpdateAvailableText { get; set; } = string.Empty;

    public MainWindowModelTranslationProperties TranslationProperties { get; } = new MainWindowModelTranslationProperties();
    private GitHubRelease? _availableGitHubRelease;

[RelayCommand]
private async Task UpdateAvailableAsync(XamlRoot xamlRoot)
{
    if (_availableGitHubRelease is null)
    {
        return;
    }

    var gitHubUpdater = new GitHubUpdater();

    await gitHubUpdater.DisplayNewUpdateDialog(
        _availableGitHubRelease,
        xamlRoot);
}

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
