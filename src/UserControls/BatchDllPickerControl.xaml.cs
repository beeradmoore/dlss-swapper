using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls;

public sealed partial class BatchDllPickerControl : UserControl
{
    internal BatchDllPickerControlModel ViewModel { get; }

    public BatchDllPickerControl(EasyContentDialog parentDialog)
    {
        this.InitializeComponent();
        ViewModel = new BatchDllPickerControlModel(parentDialog);
        DataContext = ViewModel;
    }
}
