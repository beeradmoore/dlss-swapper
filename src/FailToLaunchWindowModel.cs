using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Translations.Windows;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class FailToLaunchWindowModel : ObservableObject
{
    public FailToLaunchWindowModel() : base()
    {
        var systemDetails = new SystemDetails();
        SystemData = systemDetails.GetSystemData();
    }

    public FailToLaunchWindowTranslationPropertiesViewModel TranslationProperties { get; set; } = new FailToLaunchWindowTranslationPropertiesViewModel();

    public string SystemData { get; set; } = string.Empty;

    [RelayCommand]
    void CopyText()
    {
        var package = new DataPackage();
        package.SetText(SystemData);
        Clipboard.SetContent(package);
    }
}
