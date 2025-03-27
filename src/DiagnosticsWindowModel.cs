using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Attributes;
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
    [LanguageProperty] public string ApplicationTilteDiagnosticsWindowText => ResourceHelper.GetString("ApplicationTitle") + " - " + ResourceHelper.GetString("Diagnostics");
    [LanguageProperty] public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    #endregion

    private void OnLanguageChanged()
    {
        Type currentClassType = GetType();
        IEnumerable<string> languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
        foreach (string propertyName in languageProperties)
        {
            OnPropertyChanged(propertyName);
        }
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
