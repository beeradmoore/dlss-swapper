using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

public class DLLRecordModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ExportText => ResourceHelper.GetString("General_Export");

    [TranslationProperty]
    public string DeleteText => ResourceHelper.GetString("General_Delete");

    [TranslationProperty]
    public string DownloadText => ResourceHelper.GetString("Download");

    [TranslationProperty]
    public string DownloadErrorText => ResourceHelper.GetString("DownloadError");

    [TranslationProperty]
    public string CancelText => ResourceHelper.GetString("General_Cancel");

    [TranslationProperty]
    public string DownloadingText => ResourceHelper.GetString("Downloading");

    [TranslationProperty]
    public string RequiresDownloadText => ResourceHelper.GetString("RequiresDownload");

    [TranslationProperty]
    public string ImportedText => ResourceHelper.GetString("Imported");
}
