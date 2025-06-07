using System;
using System.Collections.Generic;
using System.Globalization;
using DLSS_Swapper.Language;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace DLSS_Swapper.Helpers;

public class ResourceHelper
{
    static readonly ResourceLoader _resourceLoader = new ResourceLoader();
    static readonly ResourceContext _resourceContext = ResourceContext.GetForViewIndependentUse();
    static readonly ResourceMap _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

    static readonly Dictionary<string, string> _resources = new Dictionary<string, string>();

    static bool _translatorMode = false;
    internal static bool TranslatorMode
    {
        get { return _translatorMode; }
        set
        {
            if (value != _translatorMode)
            {
                _translatorMode = value;
                LanguageManager.Instance.ReloadLanguage();
            }
        }
    }


    internal static void LoadResource(string key)
    {
        _resources.Clear();
        try
        {
            _resourceContext.Languages = new List<string> { key };
        }
        catch (Exception err)
        {
            Logger.Error($"Could not load resource map for key {key}: {err.Message}");
        }
    }

    internal static void UpdateFromLiveTranslations(List<TranslationRow> translations)
    {
        _resources.Clear();
        foreach (var translation in translations)
        {
            if (string.IsNullOrWhiteSpace(translation.NewTranslation))
            {
                continue;
            }
            // Add the translation to our dictionary.
            _resources[translation.Key] = translation.NewTranslation;
        }
        LanguageManager.Instance.ReloadLanguage();
    }

    public static string GetString(string resourceName)
    {
        // Load from our dictionary if we are in translator mode, but then fallback if we don't have the value.
        if (TranslatorMode && _resources.TryGetValue(resourceName, out var value))
        {
            return value;
        }

        // But if we have a resource map fall back to it.
        var resourceCandidate = _resourceMap.GetValue(resourceName, _resourceContext);
        if (string.IsNullOrWhiteSpace(resourceCandidate?.ValueAsString) == false)
        {
            return resourceCandidate.ValueAsString;
        }

        // If not we fallback to the original language.
        return _resourceLoader.GetString(resourceName);
    }

    public static string GetFormattedResourceTemplate(string templateResourceName, params object[] args)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, GetString(templateResourceName), args);
        }
        catch
        {
            return error;
        }
    }

    private const string error = "LangResourceError";
}
