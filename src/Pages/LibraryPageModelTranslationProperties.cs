using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

public class LibraryPageModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("General_ApplicationRunningAsAdmin");

    [TranslationProperty]
    public string ImportText => ResourceHelper.GetString("General_Import");

    [TranslationProperty]
    public string ExportAllText => ResourceHelper.GetString("General_ExportAll");

    [TranslationProperty]
    public string DownloadLatestText => ResourceHelper.GetString("LibraryPage_DownloadLatest");

    [TranslationProperty]
    public string RefreshText => ResourceHelper.GetString("General_Refresh");

    [TranslationProperty]
    public string WarningText => ResourceHelper.GetString("General_Warning");

    [TranslationProperty]
    public string CancelText => ResourceHelper.GetString("General_Cancel");

    [TranslationProperty]
    public string PageTitle => ResourceHelper.GetString("LibraryPage_Title");
}
