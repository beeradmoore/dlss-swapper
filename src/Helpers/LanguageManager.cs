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
            "ar-SA",
            "ar-SY",
            "ca-ES",
            "de-DE",
            "en-AU",
            "en-GB",
            "en-US",
            "es-ES",
            "fr-FR",
            "it-IT",
            "ja-JP",
            "pl-PL",
            "pt-BR",
            "ru-RU",
            "tr-TR",
            "uk-UA",
            "vi-VN",
            "zh-CN",
            "zh-TW",
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
            "ar-SA" => "اللغة العربية (المملكة العربية السعودية)", // Arabic (Saudi Arabia)
            "ar-SY" => " (سوريا) العربية", // Arabic (Syria)
            "ca-ES" => "Català", // Catalan
            "de-DE" => "Deutsch", // German (Germany)
            "en-AU" => "English (Australia)",
            "en-GB" => "English (United Kingdom)",
            "en-US" => "English (United States)",
            "es-ES" => "Español", // Spanish
            "fr-FR" => "Français", // French (France)
            "it-IT" => "Italiano", // Italian (Italy)
            "ja-JP" => "日本語", // Japanese
            "pl-PL" => "Polski", // Polish
            "pt-BR" => "Português BR", // Portuguese
            "ru-RU" => "Русский", // Russian
            "tr-TR" => "Türkçe", // Turkish
            "uk-UA" => "Українська", // Ukrainian
            "vi-VN" => "Tiếng Việt", // Vietnamese
            "zh-CN" => "简体中文", // Simplified Chinese
            "zh-TW" => "繁體中文 (臺灣)", // Traditional Chinese (Taiwan)
            _ => languageKey,
        };
    }

    public static IEnumerable<string> GetClassLanguagePropertyNames(Type classType)
    {
        return classType.GetProperties().Where(p => p.GetCustomAttribute<TranslationPropertyAttribute>() != null).Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Determines if the specified language requires Right-to-Left (RTL) text direction.
    /// </summary>
    /// <param name="languageKey">The language key (e.g., "ar-SY", "he-IL")</param>
    /// <returns>True if the language requires RTL layout, false otherwise</returns>
    public static bool IsRightToLeftLanguage(string languageKey)
    {
        if (string.IsNullOrWhiteSpace(languageKey))
            return false;
    
        // List of RTL language codes (Semitic and other RTL languages)
        var rtlLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ar",      // Arabic
            "ar-SY",   // Arabic (Syria)
            "ar-SA",   // Arabic (Saudi Arabia)
            "ar-EG",   // Arabic (Egypt)
            "ar-AE",   // Arabic (UAE)
            "ar-JO",   // Arabic (Jordan)
            "ar-LB",   // Arabic (Lebanon)
            "ar-IQ",   // Arabic (Iraq)
            "ar-KW",   // Arabic (Kuwait)
            "ar-MA",   // Arabic (Morocco)
            "ar-TN",   // Arabic (Tunisia)
            "ar-DZ",   // Arabic (Algeria)
            "he",      // Hebrew
            "he-IL",   // Hebrew (Israel)
            "fa",      // Persian/Farsi
            "fa-IR",   // Persian (Iran)
            "ur",      // Urdu
            "ur-PK",   // Urdu (Pakistan)
            "ps",      // Pashto
            "ps-AF",   // Pashto (Afghanistan)
            "sd",      // Sindhi
            "ckb",     // Central Kurdish
            "dv",      // Divehi
            "yi"       // Yiddish
        };
    
        // Check exact match first
        if (rtlLanguages.Contains(languageKey))
            return true;
    
        // Check language part only (e.g., "ar" from "ar-SY")
        var languagePart = languageKey.Split('-')[0];
        return rtlLanguages.Contains(languagePart);
    }
}
