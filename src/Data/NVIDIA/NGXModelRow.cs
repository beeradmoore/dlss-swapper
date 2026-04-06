using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Data.NVIDIA;

public partial class NGXModelRow : ObservableObject
{
    public NGXModel NGXModel { get; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = false;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public NGXModelRow(NGXModel ngxModel)
    {
        NGXModel = ngxModel;
    }
}
