using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class MultipleDLLsFoundControl : UserControl
{
    public MultipleDLLsFoundControlModel ViewModel { get; set; }

    public MultipleDLLsFoundControl(Game game, GameAssetType gameAssetType)
    {
        this.InitializeComponent();
        ViewModel = new MultipleDLLsFoundControlModel(game, gameAssetType);
        DataContext = ViewModel;
    }
}
