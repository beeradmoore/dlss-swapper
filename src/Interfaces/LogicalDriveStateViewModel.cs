using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Interfaces;
public partial class LogicalDriveStateViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string DriveLetter { get; set; }
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
}
