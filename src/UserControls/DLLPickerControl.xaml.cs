using Microsoft.UI.Xaml.Controls;
using DLSS_Swapper.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class DLLPickerControl : UserControl
{
    public DLLPickerControlModel ViewModel { get; private set; }

    public DLLPickerControl(EasyContentDialog parentDialog, Game game, GameAssetType gameAssetType)
    {
        this.InitializeComponent();

        ViewModel = new DLLPickerControlModel(parentDialog, this, game, gameAssetType);
        DataContext = ViewModel;
    }
}
