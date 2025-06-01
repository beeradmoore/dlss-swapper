using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace DLSS_Swapper.Helpers;

public class ResourceHelper
{
    static readonly ResourceLoader _resourceLoader = new ResourceLoader();
    static readonly ResourceContext _resourceContext = ResourceContext.GetForViewIndependentUse();
    static ResourceMap _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

    static readonly Dictionary<string, string> _resources = new Dictionary<string, string>();


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

        var translationsPath = Path.Combine(AppContext.BaseDirectory, "Translations", key, "Resources.resw");
        if (File.Exists(translationsPath))
        {
            try
            {
                var xDocument = XDocument.Load(translationsPath);

                foreach (var data in xDocument.Descendants("data"))
                {
                    var name = data.Attribute("name")?.Value;
                    var value = data.Element("value")?.Value;

                    // Only add the key/value if they both exist.
                    if (string.IsNullOrWhiteSpace(name) == false && string.IsNullOrWhiteSpace(value) == false)
                    {
                        _resources[name] = value;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error($"Failed to load resources from {translationsPath}: {err.Message}");
            }
        }
    }

    public static string GetString(string resourceName)
    {
        // Load from our dictionary first.
        if (_resources.TryGetValue(resourceName, out var value))
        {
            return value;
        }

        // But if we have a resource map fall back to it.
        if (_resourceMap is not null)
        {
            var resourceCandidate = _resourceMap.GetValue(resourceName, _resourceContext);
            if (resourceCandidate is not null)
            {
                return resourceCandidate.ValueAsString;
            }
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
