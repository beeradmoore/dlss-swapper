using Microsoft.UI.Xaml.Controls;

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
