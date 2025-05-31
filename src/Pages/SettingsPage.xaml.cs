using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.Pages;

/// <summary>
/// Page for application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPageModel ViewModel { get; private set; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = new SettingsPageModel(this);
        DataContext = ViewModel;
    }
}
