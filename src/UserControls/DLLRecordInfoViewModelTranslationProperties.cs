using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class DLLRecordInfoViewModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string VersionText => $"{ResourceHelper.GetString("General_Version")}: ";

    [TranslationProperty]
    public string LabelText => $"{ResourceHelper.GetString("Label")}: ";

    [TranslationProperty]
    public string FileSizeText => $"{ResourceHelper.GetString("FileSize")}: ";

    [TranslationProperty]
    public string DownloadFileSizeText => $"{ResourceHelper.GetString("DownloadFileSize")}: ";

    [TranslationProperty]
    public string FileDescriptionText => $"{ResourceHelper.GetString("FileDescription")}: ";

    [TranslationProperty]
    public string Md5Hash => $"MD5 {ResourceHelper.GetString("Hash").ToLower()}: ";

    [TranslationProperty]
    public string ZipMd5Hash => $"Zip MD5 {ResourceHelper.GetString("Hash").ToLower()}: ";
}
