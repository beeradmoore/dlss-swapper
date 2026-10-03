using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class BatchDllPickerControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string DlssGroupText => ResourceHelper.GetString("GamesPage_Batch_Group_DLSS");

    [TranslationProperty]
    public string FsrGroupText => ResourceHelper.GetString("GamesPage_Batch_Group_FSR");

    [TranslationProperty]
    public string XessGroupText => ResourceHelper.GetString("GamesPage_Batch_Group_XeSS");

    [TranslationProperty]
    public string VersionColumnText => ResourceHelper.GetString("GamesPage_Batch_ColumnHeader_Version");

    [TranslationProperty]
    public string PresetColumnText => ResourceHelper.GetString("GamesPage_Batch_ColumnHeader_Preset");

    [TranslationProperty]
    public string PresetsUnsupportedText => ResourceHelper.GetString("GamesPage_Batch_Preset_UnsupportedMessage");
}
