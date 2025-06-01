using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using DLSS_Swapper.Attributes;

namespace DLSS_Swapper.Helpers;
public class LanguageManager
{
    public event Action? OnLanguageChanged;

    static LanguageManager? _instance;
    public static LanguageManager Instance => _instance ??= new LanguageManager();


    private LanguageManager()
    {
    }

    public void ChangeLanguage(string key)
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(key);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(key);
        }
        catch (Exception err)
        {
            Logger.Error($"Failed to set Thread.CurrentThread to {key}: {err.Message}");
        }
        ResourceHelper.LoadResource(key);
        OnLanguageChanged?.Invoke();
    }

    public string[] GetKnownLanguages()
    {
        var languages = new List<string>();

        var translationsBaseDirectory = Path.Combine(AppContext.BaseDirectory, "Translations");
        var translationDirectories = Directory.GetDirectories(translationsBaseDirectory);
        foreach (var translationDirectory in translationDirectories)
        {
            var translationFile = Path.Combine(translationDirectory, "Resources.resw");
            if (Path.Exists(translationFile))
            {
                languages.Add(Path.GetFileName(translationDirectory));
            }
        }

        // If somehow this failed, always fall back to english.
        if (languages.Count == 0)
        {
            return new string[]
            {
                "en-US", // Default language
            };
        }

        return languages.ToArray();
    }

    public string GetLanguageName(string languageKey)
    {
        return languageKey switch
        {
            "en-US" => "English",
            "pl-PL" => "Polish",
            _ => languageKey,
        };
    }

    public static IEnumerable<string> GetClassLanguagePropertyNames(Type classType)
    {
        return classType.GetProperties().Where(p => p.GetCustomAttribute<TranslationPropertyAttribute>() != null).Select(p => p.Name).ToList();
    }
}
