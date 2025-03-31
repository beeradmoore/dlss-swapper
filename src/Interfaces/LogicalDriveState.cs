namespace DLSS_Swapper.Interfaces;
public class LogicalDriveState
{
    public required string DriveLetter { get; init; }
    public required bool IsEnabled { get; init; }
}
