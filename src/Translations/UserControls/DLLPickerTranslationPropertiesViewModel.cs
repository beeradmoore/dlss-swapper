using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Translations.UserControls;
public class DLLPickerTranslationPropertiesViewModel
{
    public DLLPickerTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string NoDllsFoundText => ResourceHelper.GetString("NoDllsFoundText");
    [TranslationProperty] public string PleaseNavigateLibraryToDownloadDllsText => ResourceHelper.GetString("PleaseNavigateLibraryToDownloadDllsText");
    [TranslationProperty] public string OpenDllLocationText => ResourceHelper.GetString("OpenDllLocation");
    [TranslationProperty] public string CurrentDllText => ResourceHelper.GetString("CurrentDll");
    [TranslationProperty] public string OriginalDllRestoreText => ResourceHelper.GetString("OriginalDllRestore");
    [TranslationProperty] public string OriginalDllText => ResourceHelper.GetString("OriginalDllText");
}
