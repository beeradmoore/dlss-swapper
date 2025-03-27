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
    private LanguageManager() { }

    public static LanguageManager Instance => _instance.Value;
    public void ChangeLanguage(string key)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo(key);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(key);
        OnLanguageChanged.Invoke();
    }

    public static IEnumerable<string> GetClassLanguagePropertyNames(Type classType)
    {
        return classType.GetProperties().Where(p => p.GetCustomAttribute<LanguagePropertyAttribute>() != null).Select(p => p.Name).ToList();
    }

    public event Action OnLanguageChanged;

    private static readonly Lazy<LanguageManager> _instance = new Lazy<LanguageManager>(() => new LanguageManager());
}
