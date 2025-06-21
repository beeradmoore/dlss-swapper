using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class TranslationToolsWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteTranslationToolsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("TranslationTools_WindowTitle")}";

    [TranslationProperty]
    public string SourceLanguageText => ResourceHelper.GetString("TranslationTools_SourceLanguage");

    [TranslationProperty]
    public string ImportAsTrasnlationText => ResourceHelper.GetString("TranslationTools_ImportAsTranslation");

    [TranslationProperty]
    public string ReloadAppText => ResourceHelper.GetString("TranslationTools_ReloadApp");

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
    public string SourceTranslationText => ResourceHelper.GetString("TranslationTools_SourceTranslation");

    [TranslationProperty]
    public string NewTranslationText => ResourceHelper.GetString("TranslationTools_NewTranslation");
}
