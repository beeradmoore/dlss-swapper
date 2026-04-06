using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class ProxySettingsControl : UserControl
{
    public ProxySettingsModel ViewModel { get; }
    public ProxySettingsControl()
    {
        InitializeComponent();

        ViewModel = new ProxySettingsModel();
        DataContext = ViewModel;
    }
}
