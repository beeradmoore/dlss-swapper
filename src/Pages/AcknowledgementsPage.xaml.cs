using Microsoft.UI.Xaml.Controls;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AcknowledgementsPage : Page
{
    public AcknowledgementsPageModel ViewModel { get; private set; }
    public AcknowledgementsPage()
    {
        this.InitializeComponent();
        ViewModel = new AcknowledgementsPageModel(this);
    }
}
