using System.Globalization;
using System.Resources;

namespace DLSS_Swapper.Helpers;
public class ResourceHelper
{
    static ResourceManager? _resourceManager;

    public static string GetString(string resourceName)
    {
        if (_resourceManager is null)
        {
            _resourceManager = new ResourceManager("DLSS_Swapper.Languages.Resources", typeof(ResourceHelper).Assembly);
        }

        return _resourceManager.GetString(resourceName, CultureInfo.CurrentUICulture) ?? error;
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
