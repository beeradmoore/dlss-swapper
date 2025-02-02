using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class NewDLLsControl : UserControl
{
    public NewDLLsControlModel ViewModel { get; private set; }

    public NewDLLsControl()
    {
        this.InitializeComponent();
        ViewModel = new NewDLLsControlModel();
        DataContext = ViewModel;
    }
}
