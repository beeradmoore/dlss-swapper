using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Helpers;
public class ResourceHelper
{
    private const string error = "LangResourceError";

    public static string GetString(string resourceName)
    {
        ResourceManager rm = new ResourceManager("DLSS_Swapper.Languages.Resources", typeof(ResourceHelper).Assembly);
        return rm.GetString(resourceName, CultureInfo.CurrentUICulture) ?? error;
    }

    //Ommit exceptions and
    public static string FormattedResourceTemplate(string templateResourceName, params object[] args)
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
}
