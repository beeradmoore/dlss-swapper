using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

    internal void ReloadLanguage()
    {
        OnLanguageChanged?.Invoke();
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
        ReloadLanguage();
    }

    public string[] GetKnownLanguages()
    {
        // For now this is a hardcoded list. It would be nice to dynamically discover from resource class or something.
        return [
            "en-US",
            "pl-PL",
            "zh-CN",
#if DEBUG
            "LANG_HUNT",
#endif
        ];
    }

    public string GetLanguageName(string languageKey)
    {
        // For now this is a hardcoded list. It would be nice to dynamically discover from resource class or something.
        return languageKey switch
        {
            "en-US" => "English",
            "pl-PL" => "Polish",
            "zh-CN" => "简体中文",

            _ => languageKey,
        };
    }

    public static IEnumerable<string> GetClassLanguagePropertyNames(Type classType)
    {
        return classType.GetProperties().Where(p => p.GetCustomAttribute<TranslationPropertyAttribute>() != null).Select(p => p.Name).ToList();
    }
}
