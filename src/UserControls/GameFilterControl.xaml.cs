using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls;

public sealed partial class GameFilterControl : UserControl
{
    private GameFilterControlViewModel ViewModel { get; }

    public GameFilterControl()
    {
        this.InitializeComponent();
        ViewModel = new GameFilterControlViewModel();
        DataContext = ViewModel;
    }
}
