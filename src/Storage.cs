using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Helpers;
using DLSS_Swapper.Data;

namespace DLSS_Swapper
{
    // TODO: Test portable app.
    // TODO: Clean portable temp path on launch

    /*
     * For notes on where data is stored please see https://github.com/beeradmoore/dlss-swapper/wiki/Local-Data-Structure 
     */
    static class Storage
    {

        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

#if WINDOWS_STORE
        static string storagePath => Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif PORTABLE
        static string storagePath => Path.Combine(Directory.GetCurrentDirectory(), "StoredData");
#else
        static string storagePath => Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper");
#endif


        static Storage()
        {
#if !WINDOWS_STORE
            // Create directories if they doesn't exist.
            //CreateDirectoryIfNotExists(GetTemp());
            CreateDirectoryIfNotExists(GetStorageFolder());
            CreateDirectoryIfNotExists(GetDynamicJsonFolder());
#endif
        }


        public static string GetTemp()
        {
#if WINDOWS_STORE
            var path = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;
#elif PORTABLE
            var path = Path.Combine(storagePath, "temp");
            CreateDirectoryIfNotExists(path);
#else
            var path = Path.Combine(Path.GetTempPath(), "DLSS Swapper");
            CreateDirectoryIfNotExists(path);
#endif
            return path;
        }

        public static string GetStorageFolder()
        {
            return storagePath;
        }

        static string GetStaticJsonFolder()
        {
#if WINDOWS_STORE
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoredData", "static_json");
#elif PORTABLE
            return Path.Combine(Directory.GetCurrentDirectory(), "StoredData", "static_json");
#else
            return Path.Combine(Directory.GetCurrentDirectory(), "StoredData", "static_json"); ;
#endif
        }

        static string GetDynamicJsonFolder()
        {
            return Path.Combine(storagePath, "json");
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
                    await JsonSerializer.SerializeAsync(stream, settings, jsonSerializerOptions);
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
        internal static async Task<Settings> LoadSettingsJsonAsync()
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
                    return await JsonSerializer.DeserializeAsync<Settings>(stream);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }
        }

#if !WINDOWS_STORE
        /// <summary>
        /// Saves DLSSRecords object to the known position for a dynamic dlss_records.json file. This is excluded from the Windows Store build as there is no dynamic dlss_records.json file. 
        /// </summary>
        /// <param name="items">DLSSRecords object to be saved</param>
        /// <returns>true if the object was saved</returns>
        internal static async Task<bool> SaveDLSSRecordsJsonAsync(DLSSRecords items)
        {
            var dlssRecordsFile = Path.Combine(GetDynamicJsonFolder(), "dlss_records.json");
            try
            {
                using (var stream = File.Open(dlssRecordsFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, items, jsonSerializerOptions);
                }
                return true;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return false;
            }
        }
#endif


        /// <summary>
        /// Loads dlss_records.json file. Will attempt to load dynamic file first and then static if dyanmic does not exist.
        /// </summary>
        /// <returns>DLSSRecords object. This object could be null if we failed to load DLSS records</returns>
        internal static async Task<DLSSRecords> LoadDLSSRecordsJsonAsync()
        {  
            // No dyanmic dlss_records.json file for Windows Store so don't even bother loading it.
            var dlssRecordsFile = Path.Combine(GetDynamicJsonFolder(), "dlss_records.json");
#if !WINDOWS_STORE
          
            if (File.Exists(dlssRecordsFile))
            {
                try
                {
                    using (var stream = File.Open(dlssRecordsFile, FileMode.Open))
                    {
                        var dlssRecords = await JsonSerializer.DeserializeAsync<DLSSRecords>(stream);
                        if (dlssRecords != null)
                        {
                            return dlssRecords;
                        } 
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return new DLSSRecords();
                }
            }
#endif

            // If we got to here there is no dynamic dlss_records.json file to load, so load static one instead.
            dlssRecordsFile = Path.Combine(GetStaticJsonFolder(), "dlss_records.json");
            if (File.Exists(dlssRecordsFile))
            {
                try
                {
                    using (var stream = File.Open(dlssRecordsFile, FileMode.Open))
                    {
                        var dlssRecords = await JsonSerializer.DeserializeAsync<DLSSRecords>(stream);
                        if (dlssRecords != null)
                        {
                            return dlssRecords;
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return new DLSSRecords();
                }
            }
            else
            {
                Logger.Error("There was no static dlss_records.json file to load.");
                return new DLSSRecords();
            }

            // If we got here it means we failed to deserialize the dlss_records.json
            Logger.Error("There an issue attempting to load dlss_records.json.");
            return new DLSSRecords();
        }

        /// <summary>
        /// Loads a list of imported DLSS records.
        /// </summary>
        /// <returns>List of imported DLSS records. This list will be empty if no imported recrods were found.</returns>
        internal static async Task<List<DLSSRecord>> LoadImportedDLSSRecordsJsonAsync()
        {
            var importedDLSSRecordsFile = Path.Combine(GetDynamicJsonFolder(), "imported_dlss_records.json");
            if (File.Exists(importedDLSSRecordsFile) == true)
            {
                try
                {
                    using (var stream = File.Open(importedDLSSRecordsFile, FileMode.Open))
                    {
                        var importedDLSSRecords = await JsonSerializer.DeserializeAsync<List<DLSSRecord>>(stream);
                        if (importedDLSSRecords != null)
                        {
                            return importedDLSSRecords;
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }
            }

            // If we failed to load we return a blank list.
            return new List<DLSSRecord>();
        }

        /// <summary>
        /// Saves the imported DLSS records to imported_dlss_records.json
        /// </summary>
        /// <returns></returns>
        internal static async Task<bool> SaveImportedDLSSRecordsJsonAsync()
        {
            var importedDLSSRecordsFile = Path.Combine(GetDynamicJsonFolder(), "imported_dlss_records.json");
            CreateDirectoryForFileIfNotExists(importedDLSSRecordsFile);
            try
            {
                using (var stream = File.Open(importedDLSSRecordsFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, App.CurrentApp.ImportedDLSSRecords, jsonSerializerOptions);
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
