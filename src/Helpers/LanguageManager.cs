using System;
using System.Globalization;
using System.Threading;

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

    public event Action OnLanguageChanged;

    private static readonly Lazy<LanguageManager> _instance = new Lazy<LanguageManager>(() => new LanguageManager());
}
