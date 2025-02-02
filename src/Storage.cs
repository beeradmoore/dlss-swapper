using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Data;

namespace DLSS_Swapper
{
    // TODO: Test portable app.
    // TODO: Clean portable temp path on launch

    /*
     * For notes on where data is stored please see https://github.com/beeradmoore/dlss-swapper/wiki/Local-Data-Structure
     */
    internal static class Storage
    {
        private static JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            TypeInfoResolver = SourceGenerationContext.Default,
        };
        private static string? _storagePath;
#if   PORTABLE == true  && DEBUG == true
        //public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData", "DEBUG", Guid.NewGuid().ToString());
        public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData", "DEBUG");
#elif PORTABLE == true  && DEBUG == false
        public static string StoragePath => _storagePath ??= Path.Combine(AppContext.BaseDirectory, "StoredData");
#elif PORTABLE == false && DEBUG == true
        //public static string StoragePath => _storagePath ??= Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper", "DEBUG", Guid.NewGuid().ToString());
        public static string StoragePath => _storagePath ??= Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper", "DEBUG");
#elif PORTABLE == false && DEBUG == false
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

        private static string GetDynamicJsonFolder()
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

        /// <summary>
        /// When given a file path it will make the directory structure so that file is ready to be created in. A directory should not be passed to this. Use CreateDirectoryIfNotExists instead for that.
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>True if the directory could be created</returns>
        public static bool CreateDirectoryForFileIfNotExists(string path)
        {
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
                Logger.Error(err.Message);
                return false;
            }
        }

        /// <summary>
        /// Saves teh current settings object to settings.json in the apps dynamic json folder.
        /// </summary>
        /// <param name="settings">Settings object to be saved</param>
        /// <returns>Task</returns>
        internal static async Task SaveSettingsJsonAsync(Settings settings)
        {
            var settingsFile = Path.Combine(GetDynamicJsonFolder(), "settings.json");
            try
            {
                using (var stream = File.Open(settingsFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, settings, SourceGenerationContext.Default.Settings);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        /// <summary>
        /// Loads settings from settings.json in the apps dynamic json folder.
        /// </summary>
        /// <returns>Settings object, or null if it could not be loaded</returns>
        internal static async Task<Settings?> LoadSettingsJsonAsync()
        {
            var settingsFile = Path.Combine(GetDynamicJsonFolder(), "settings.json");

            // If the settings file doesn't exist we return null to default it elsewhere.
            if (File.Exists(settingsFile) == false)
            {
                return null;
            }

            try
            {
                using (var stream = File.Open(settingsFile, FileMode.Open))
                {
                    return await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Settings);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }
        }

        /// <summary>
        /// Saves Manifest object to the known position for a dynamic manifest.json file.
        /// </summary>
        /// <param name="manifest">Manifest object to be saved</param>
        /// <returns>true if the object was saved</returns>
        // Previously: SaveDLSSRecordsJsonAsync
        internal static async Task<bool> SaveManifestJsonAsync(Manifest manifest)
        {
            var dlssRecordsFile = Path.Combine(GetDynamicJsonFolder(), "manifest.json");
            try
            {
                using (var stream = File.Open(dlssRecordsFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, manifest, SourceGenerationContext.Default.Manifest);
                }

                return true;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return false;
            }
        }

        /// <summary>
        /// Loads manifest.json file. Will attempt to load dynamic file first and then static if dyanmic does not exist.
        /// </summary>
        /// <returns>Manifest object. This could be a blank object if we failed to load DLL records</returns>
        internal static async Task<Manifest> LoadManifestJsonAsync()
        {
            var manifestFile = Path.Combine(GetDynamicJsonFolder(), "manifest.json");

            if (File.Exists(manifestFile))
            {
                try
                {
                    using (var stream = File.Open(manifestFile, FileMode.Open))
                    {
                        var manifest = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Manifest);
                        if (manifest is not null)
                        {
                            return manifest;
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return new Manifest();
                }
            }

            // If we got to here there is no dynamic manifest.json file to load, so load static one instead.
            try
            {
                using (var staticManifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLSS_Swapper.Assets.static_manifest.json"))
                {
                    if (staticManifestStream is not null)
                    {
                        var manifest = await JsonSerializer.DeserializeAsync(staticManifestStream, SourceGenerationContext.Default.Manifest);
                        if (manifest is not null)
                        {
                            Logger.Info("Loaded static manifest");
                            return manifest;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return new Manifest();
            }

            // If we got here it means we failed to deserialize the manifest.json
            Logger.Error("There an issue attempting to load manifest.json.");
            return new Manifest();
        }

        /// <summary>
        /// Loads an imported manifest file of all imported DLL records.
        /// </summary>
        /// <returns>Manifest object of imported DLL records. This will contain empty lists if no imported recrods were found.</returns>
        // Previously: LoadImportedDLSSRecordsJsonAsync
        internal static async Task<Manifest> LoadImportedManifestJsonAsync()
        {
            var importedDLSSRecordsFile = Path.Combine(GetDynamicJsonFolder(), "imported_manifest.json");
            if (File.Exists(importedDLSSRecordsFile))
            {
                try
                {
                    using (var stream = File.Open(importedDLSSRecordsFile, FileMode.Open))
                    {
                        var importedManifest = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Manifest);
                        if (importedManifest is not null)
                        {
                            return importedManifest;
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }
            }

            // If we failed to load we return a blank manifest.
            return new Manifest();
        }

        /// <summary>
        /// Saves the imported DLSS records to imported_manifest.json
        /// </summary>
        /// <returns></returns>
        // Previously: SaveImportedDLSSRecordsJsonAsync
        internal static async Task<bool> SaveImportedManifestJsonAsync()
        {
            var importedManifestFile = Path.Combine(GetDynamicJsonFolder(), "imported_manifest.json");
            CreateDirectoryForFileIfNotExists(importedManifestFile);
            try
            {
                using (var stream = File.Open(importedManifestFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, App.CurrentApp.ImportedManifest, SourceGenerationContext.Default.Manifest);
                }

                return true;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return false;
            }
        }
    }
}
