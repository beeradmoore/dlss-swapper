using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class DiagnosticsWindowModel : ObservableObject, IDisposable
{
    public DiagnosticsWindowModel()
    {
        var systemDetails = new SystemDetails();
        DiagnosticsLog = $"{systemDetails.GetSystemData()}\n\n{systemDetails.GetLibraryData()}\n";
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
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
    public string ApplicationTilteDiagnosticsWindowText => ResourceHelper.GetString("ApplicationTitle") + " - " + ResourceHelper.GetString("Diagnostics");
    public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    #endregion

    private void OnLanguageChanged()
    {
        OnPropertyChanged(ApplicationTilteDiagnosticsWindowText);
        OnPropertyChanged(ClickToCopyDetailsText);
    }

    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    ~DiagnosticsWindowModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
