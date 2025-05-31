using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations.Pages;
public class LibraryPageTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public LibraryPageTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("ApplicationRunsInAdministrativeModeInfo");
    [TranslationProperty] public string ImportText => ResourceHelper.GetString("Import");
    [TranslationProperty] public string ExportAllText => ResourceHelper.GetString("ExportAll");
    [TranslationProperty] public string RefreshText => ResourceHelper.GetString("Refresh");
    [TranslationProperty] public string WarningText => ResourceHelper.GetString("Warning");
    [TranslationProperty] public string CancelText => ResourceHelper.GetString("Cancel");
    [TranslationProperty] public string LibraryText => ResourceHelper.GetString("Library");
}
