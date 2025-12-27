using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DLSS_Swapper;

// TODO: Test portable app.
// TODO: Clean portable temp path on launch

/*
 * For notes on where data is stored please see https://github.com/beeradmoore/dlss-swapper/wiki/Local-Data-Structure 
 */
static class Storage
{

    static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        TypeInfoResolver = SourceGenerationContext.Default,
    };

    static string? _storagePath;
#if   PORTABLE && DEBUG
    //public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData", "DEBUG", Guid.NewGuid().ToString());
    public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData", "DEBUG");
#elif PORTABLE && !DEBUG
    public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData");
#elif !PORTABLE && DEBUG
    //public static string StoragePath => _storagePath ??= Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper", "DEBUG", Guid.NewGuid().ToString());
    public static string StoragePath => _storagePath ??= Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper", "DEBUG");
#elif !PORTABLE && !DEBUG
    public static string StoragePath => _storagePath  ??= Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper");
#endif


    static Storage()
    {
        // Create directories if they doesn't exist.
        //CreateDirectoryIfNotExists(GetTemp());
        CreateDirectoryIfNotExists(GetStorageFolder());
        CreateDirectoryIfNotExists(GetDynamicJsonFolder());
        CreateDirectoryIfNotExists(GetImageCachePath());
    }

    public static string GetTemp()
    {
#if PORTABLE
        var path = Path.Combine(StoragePath, "temp");
        CreateDirectoryIfNotExists(path);
#else
        var path = Path.Combine(Path.GetTempPath(), "DLSS Swapper");
        CreateDirectoryIfNotExists(path);
#endif
        return path;
    }

    public static string GetStorageFolder()
    {
        return StoragePath;
    }

    public static string GetDynamicJsonFolder()
    {
        return Path.Combine(StoragePath, "json");
    }

    public static string GetDBPath()
    {
        CreateDirectoryIfNotExists(StoragePath);
        return Path.Combine(StoragePath, "dlss_swapper.db");
    }

    public static string GetImageCachePath()
    {
        return Path.Combine(StoragePath, "image_cache");
    }

    public static string GetReleasesPath()
    {
        return Path.Combine(GetDynamicJsonFolder(), "releases.json");
    }

    public static string GetManifestPath()
    {
        return Path.Combine(GetDynamicJsonFolder(), "manifest.json");
    }

    public static string GetImportedManifestPath()
    {
        return Path.Combine(GetDynamicJsonFolder(), "imported_manifest.json");
    }

    /// <summary>
    /// When given a file path it will make the directory structure so that file is ready to be created in. A directory should not be passed to this. Use CreateDirectoryIfNotExists instead for that.
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>True if the directory could be created</returns>
    public static bool CreateDirectoryForFileIfNotExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Logger.Error("A path should not be empty in CreateDirectoryForFileIfNotExists");
            return false;
        }

        if (Directory.Exists(path))
        {
            Logger.Error("A directory should not be passed to CreateDirectoryForFileIfNotExists");
            return false;
        }
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            Logger.Error("A directory should not be empty in CreateDirectoryForFileIfNotExists");
            return false;
        }
        return CreateDirectoryIfNotExists(directory);
    }

    /// <summary>
    /// Creates a directory if it doesn't already exist.
    /// </summary>
    /// <param name="directory">Directory to be created</param>
    /// <returns>True if the directory could be created</returns>
    public static bool CreateDirectoryIfNotExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        try
        {
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return false;
        }
    }

    /// <summary>
    /// Saves the current settings object to settings.json in the apps dynamic json folder.
    /// </summary>
    /// <param name="settings">Settings object to be saved</param>
    /// <returns>Task</returns>
    internal static void SaveSettingsJson(Settings settings)
    {
        var settingsFile = Path.Combine(GetDynamicJsonFolder(), "settings.json");
        try
        {
            using (var stream = File.Open(settingsFile, FileMode.Create))
            {
                JsonSerializer.Serialize(stream, settings, SourceGenerationContext.Default.Settings);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
        }
    }

    /// <summary>
    /// Loads settings from settings.json in the apps dynamic json folder.
    /// </summary>
    /// <returns>Settings object, or null if it could not be loaded</returns>
    internal static Settings? LoadSettingsJson()
    {
        var settingsFile = Path.Combine(GetDynamicJsonFolder(), "settings.json");

        // If the settings file doesn't exist we return null to default it elsewhere.
        if (File.Exists(settingsFile) == false)
        {
            return null;
        }

        try
        {
            using (var stream = File.OpenRead(settingsFile))
            {
                return JsonSerializer.Deserialize(stream, SourceGenerationContext.Default.Settings);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return null;
        }
    }
}
