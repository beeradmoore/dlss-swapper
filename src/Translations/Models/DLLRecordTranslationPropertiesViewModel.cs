using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.Attributes;

namespace DLSS_Swapper.Translations.Models;

public class DLLRecordTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public DLLRecordTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string ExportText => ResourceHelper.GetString("Export");
    [TranslationProperty] public string DeleteText => ResourceHelper.GetString("Delete");
    [TranslationProperty] public string DownloadText => ResourceHelper.GetString("Download");
    [TranslationProperty] public string DownloadErrorText => ResourceHelper.GetString("DownloadError");
    [TranslationProperty] public string CancelText => ResourceHelper.GetString("Cancel");
    [TranslationProperty] public string DownloadingText => ResourceHelper.GetString("Downloading");
    [TranslationProperty] public string RequiresDownloadText => ResourceHelper.GetString("RequiresDownload");
    [TranslationProperty] public string ImportedText => ResourceHelper.GetString("Imported");
}
