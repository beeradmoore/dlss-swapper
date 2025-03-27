using Microsoft.UI.Xaml;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindowModel ViewModel { get; private set; }

    public DiagnosticsWindow()
    {
        this.InitializeComponent();
        ViewModel = new DiagnosticsWindowModel();
    }
}
