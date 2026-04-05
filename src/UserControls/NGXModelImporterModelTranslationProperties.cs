using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class NGXModelImporterModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ColumnHeaderAssetTypeText => ResourceHelper.GetString("GameHistoryControl_AssetTypeHeader");

    [TranslationProperty]
    public string ColumnHeaderVersionText => ResourceHelper.GetString("General_Version");
}
