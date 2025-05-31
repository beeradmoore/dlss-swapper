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

    public void ChangeLanguage(string key)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo(key);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(key);
        OnLanguageChanged?.Invoke();
    }

    // TODO: Change this to be dynamic.
    public string[] GetKnownLanguages()
    {
        return new string[]
        {
            "en-US",
            "pl-PL",
        };
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
