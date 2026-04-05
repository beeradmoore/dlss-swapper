using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Data.NVIDIA;

public partial class NGXModelRow : ObservableObject
{
    public NGXModel NGXModel { get; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = false;

    // TODO: Disable rows for already imported content?
    public bool IsEnabled { get; } = true;

    public NGXModelRow(NGXModel ngxModel)
    {
        NGXModel = ngxModel;
    }
}
