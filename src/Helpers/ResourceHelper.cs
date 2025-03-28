using System.Globalization;
using System.Resources;

namespace DLSS_Swapper.Helpers;
public class ResourceHelper
{
    public static string GetString(string resourceName)
    {
        ResourceManager rm = new ResourceManager("DLSS_Swapper.Languages.Resources", typeof(ResourceHelper).Assembly);
        return rm.GetString(resourceName, CultureInfo.CurrentUICulture) ?? error;
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
