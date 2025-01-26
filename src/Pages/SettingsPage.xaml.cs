using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.Pages;

/// <summary>
/// Page for application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    internal SettingsPageModel ViewModel { get; init; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = new SettingsPageModel(this);
        DataContext = ViewModel;
    }   
}
