using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class TranslationToolsWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteTranslationToolsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("TranslationToolsPage_WindowTitle")}";

    [TranslationProperty]
    public string SourceLanguageText => $"{ResourceHelper.GetString("TranslationToolsPage_SourceLanguage")}: ";

    [TranslationProperty]
    public string ImportAsTrasnlationText => ResourceHelper.GetString("TranslationToolsPage_ImportAsTranslation");

    [TranslationProperty]
    public string LoadExistingTranslationText => ResourceHelper.GetString("TranslationToolsPage_LoadExistingTranslation");

    [TranslationProperty]
    public string ReloadAppText => ResourceHelper.GetString("TranslationToolsPage_ReloadApp");

    [TranslationProperty]
    public string LoadText => ResourceHelper.GetString("General_Load");

    [TranslationProperty]
    public string SaveText => ResourceHelper.GetString("General_Save");

    [TranslationProperty]
    public string PublishText => ResourceHelper.GetString("General_Publish");

    [TranslationProperty]
    public string KeyText => ResourceHelper.GetString("TranslationToolsPage_Key");

    [TranslationProperty]
    public string CommentText => ResourceHelper.GetString("TranslationToolsPage_Comment");

    [TranslationProperty]
    public string SourceTranslationText => ResourceHelper.GetString("TranslationToolsPage_SourceTranslation");

    [TranslationProperty]
    public string NewTranslationText => ResourceHelper.GetString("TranslationToolsPage_NewTranslation");

    [TranslationProperty]
    public string TranslationProgressText => $"{ResourceHelper.GetString("TranslationToolsPage_TranslationProgress")}: ";
}
