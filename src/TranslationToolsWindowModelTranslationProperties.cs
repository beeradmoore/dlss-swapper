using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class TranslationToolsWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteTranslationToolsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("TranslationTools")}";

    [TranslationProperty]
    public string SourceLanguageText => ResourceHelper.GetString("SourceLanguage");

    [TranslationProperty]
    public string ImportAsTrasnlationText => ResourceHelper.GetString("ImportAsTrasnlation");

    [TranslationProperty]
    public string ReloadAppText => ResourceHelper.GetString("ReloadApp");

    [TranslationProperty]
    public string LoadText => ResourceHelper.GetString("Load");

    [TranslationProperty]
    public string SaveText => ResourceHelper.GetString("Save");

    [TranslationProperty]
    public string PublishText => ResourceHelper.GetString("Publish");

    [TranslationProperty]
    public string KeyText => ResourceHelper.GetString("Key");

    [TranslationProperty]
    public string CommentText => ResourceHelper.GetString("Comment");

    [TranslationProperty]
    public string SourceTranslationText => ResourceHelper.GetString("SourceTranslation");

    [TranslationProperty]
    public string NewTranslationText => ResourceHelper.GetString("NewTranslation");
}
