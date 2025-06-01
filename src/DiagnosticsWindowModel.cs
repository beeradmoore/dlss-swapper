using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Translations.Windows;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class DiagnosticsWindowModel : ObservableObject
{
    public DiagnosticsWindowModel() : base()
    {
        var systemDetails = new SystemDetails();
        DiagnosticsLog = $"{systemDetails.GetSystemData()}\n\n{systemDetails.GetLibraryData()}\n";
    }

    public DiagnosticsWindowTranslationPropertiesViewModel TranslationProperties { get; } = new DiagnosticsWindowTranslationPropertiesViewModel();

    public string DiagnosticsLog { get; set; } = string.Empty;

    [RelayCommand]
    void CopyText()
    {
        var package = new DataPackage();
        package.SetText(DiagnosticsLog);
        Clipboard.SetContent(package);
    }

}
