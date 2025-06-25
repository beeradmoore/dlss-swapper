using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class TranslationToolboxWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteTranslationToolboxWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("TranslationToolboxPage_WindowTitle")}";

    [TranslationProperty]
    public string SourceLanguageText => $"{ResourceHelper.GetString("TranslationToolboxPage_SourceLanguage")}: ";

    [TranslationProperty]
    public string ImportAsTrasnlationText => ResourceHelper.GetString("TranslationToolboxPage_ImportAsTranslation");

    [TranslationProperty]
    public string LoadExistingTranslationText => ResourceHelper.GetString("TranslationToolboxPage_LoadExistingTranslation");

    [TranslationProperty]
    public string ReloadAppText => ResourceHelper.GetString("TranslationToolboxPage_ReloadApp");

    [TranslationProperty]
    public string LoadText => ResourceHelper.GetString("General_Load");

    [TranslationProperty]
    public string SaveText => ResourceHelper.GetString("General_Save");

    [TranslationProperty]
    public string PublishText => ResourceHelper.GetString("General_Publish");

    [TranslationProperty]
    public string HelpText => ResourceHelper.GetString("General_Help");

    [TranslationProperty]
    public string KeyText => ResourceHelper.GetString("TranslationToolboxPage_Key");

    [TranslationProperty]
    public string CommentText => ResourceHelper.GetString("TranslationToolboxPage_Comment");

    [TranslationProperty]
    public string SourceTranslationText => ResourceHelper.GetString("TranslationToolboxPage_SourceTranslation");

    [TranslationProperty]
    public string NewTranslationText => ResourceHelper.GetString("TranslationToolboxPage_NewTranslation");

    [TranslationProperty]
    public string TranslationProgressText => $"{ResourceHelper.GetString("TranslationToolboxPage_TranslationProgress")}: ";
}
