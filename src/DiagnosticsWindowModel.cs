using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class DiagnosticsWindowModel : LocalizedViewModelBase
{
    public DiagnosticsWindowModel() : base()
    {
        var systemDetails = new SystemDetails();
        DiagnosticsLog = $"{systemDetails.GetSystemData()}\n\n{systemDetails.GetLibraryData()}\n";
    }

    public string DiagnosticsLog { get; set; } = string.Empty;

    [RelayCommand]
    void CopyText()
    {
        var package = new DataPackage();
        package.SetText(DiagnosticsLog);
        Clipboard.SetContent(package);
    }

    #region TranslationProperties
    [LanguageProperty] public string ApplicationTilteDiagnosticsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("Diagnostics")}";
    [LanguageProperty] public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    #endregion
}
