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
    /*
     * File storage notes:
     * static DLSS zips - Zip files included with the app package.
     * downloaded DLSS zips - Zip files downloaded with the inbuilt DLSS downloader on the library page
     * imported DLSS zips - Zip files of DLSS records imported via the library page
     * static json - Location of json files that do not change. All apps will contain the latest dlss_records.json in the event there is no
     *               internet connectivity to download a fresh version. Windows Store release does not download an updated dlss_records.json so
     *               what you get is what was bundled with release, and for this type of release all current DLSS records should also be
     *               included in the static DLSS zips folder.
     * dynamic json - Location of json files that do change (eg. settings.json, imported_dlss_records.json)
     * temp - Location of temp downloads
     * 
     * Windows Store:
     * static DLSS zips - StoredData\dlss_zip\
     * downloaded DLSS zips - (does not exist)
     * imported DLSS zips - %LOCALAPPDATA%\Packages\{package_id}\LocalState\imported_dlss_zip\
     * static json - StoredData\static_json\
     * dynamic json - %LOCALAPPDATA%\Packages\{package_id}\LocalState\json\
     * temp - %LOCALAPPDATA%\Packages\{package_id}\TempState
     * 
     * Unpackaged:
     * static DLSS zips - (does not exist)
     * downloaded DLSS zips - %LOCALAPPDATA%\DLSS Swapper\dlss_zip\
     * imported DLSS zips - %LOCALAPPDATA%\DLSS Swapper\imported_dlss_zip\
     * static json - StoredData\static_json\
     * dynamic json - %LOCALAPPDATA%\DLSS Swapper\json\
     * temp - %LOCALAPPDATA%\Temp\
     * 
     * Portable:
     * static DLSS zips - (does not exist)
     * downloaded DLSS zips - StoredData\dlss_zip\
     * imported DLSS zips - StoredData\imported_dlss_zip\
     * static json - StoredData\static_json\
     * dynamic json - StoredData\json\ 
     * temp - StoredData\temp\
     * 
     * */
    static class Storage
    {

        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

#if !WINDOWS_STORE
        // TODO: Test portable app.
#if PORTABLE
        static string unpackagedStoragePath => Path.Combine(Directory.GetCurrentDirectory(), "StoredData");
#else
        static string unpackagedStoragePath => Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper");
#endif
#endif

        static Storage()
        {
#if !WINDOWS_STORE
            try
            {
                // Create storage directory if it doesn't exist.
                if (Directory.Exists(unpackagedStoragePath) == false)
                {
                    Directory.CreateDirectory(unpackagedStoragePath);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
#endif
        }



        private static async Task<T> LoadJsonAsync<T>(string filename) where T : class
        {
            Debugger.Break();
#if WINDOWS_STORE

            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            var fullPath = Path.Combine(AppContext.BaseDirectory, "StoredData", filename);
            if (File.Exists(fullPath) == false)
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }


            /*
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (await storageFolder.FileExistsAsync(filename) == false)
                {
                    return null;
                }

                var storageFile = await storageFolder.GetFileAsync(filename);
                using (var inputStream = await storageFile.OpenSequentialReadAsync())
                {
                    using (var stream = inputStream.AsStreamForRead())
                    {
                        return await JsonSerializer.DeserializeAsync<T>(stream);
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }
            */
#else


            var fullPath = Path.Combine(unpackagedStoragePath, filename);
            if (File.Exists(fullPath) == false)
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }
#endif
        }



        static async Task<bool> SaveJsonAsync<T>(T obj, string filename) where T : class
        {
            try
            {
#if WINDOWS_STORE
                var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var storageFile = await storageFolder.GetFileAsync(filename);
                using (var stream = await storageFile.OpenStreamForWriteAsync())
                {
                    await JsonSerializer.SerializeAsync<T>(stream, obj, jsonSerializerOptions);
                }
#else 
                var fullPath = Path.Combine(unpackagedStoragePath, filename);
                using (var stream = File.Open(fullPath, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync<T>(stream, obj, jsonSerializerOptions);
                }
#endif
                return true;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return false;
            }
        }

        public static string GetTemp()
        {
#if WINDOWS_STORE
            return Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;
#elif PORTABLE
            // TODO: Portable store in StoredData\temp\
            // Need to manually clean this on app launch.
#else
            var tempDLSSSwapperPath = Path.Combine(Path.GetTempPath(), "DLSS Swapper");
            CreateDirectoryIfNotExists(tempDLSSSwapperPath);
            return tempDLSSSwapperPath;
#endif
        }

        public static string GetStorageFolder()
        {
#if WINDOWS_STORE
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#else
            return unpackagedStoragePath;
#endif
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
#if WINDOWS_STORE
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "json");
#elif PORTABLE
            return Path.Combine(Directory.GetCurrentDirectory(), "StoredData", "json");
#else
            return Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "DLSS Swapper", "json"); ;
#endif
        }

        public static string CreateFolderAsync(string path)
        {
#if WINDOWS_STORE
            var createdPath = AsyncHelper.RunSync(() => Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists).AsTask());
            return createdPath.Path;
#else
            var desiredPath = Path.Combine(unpackagedStoragePath, path);
            if (Directory.Exists(desiredPath) == false)
            {
                Directory.CreateDirectory(desiredPath);
            }
            return desiredPath;
#endif
            // , Windows.Storage.CreationCollisionOption.OpenIfExists
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

        internal static async Task SaveSettingsJsonAsync(Settings settings)
        {
            var settingsFile = Path.Combine(GetDynamicJsonFolder(), "settings.json");
            CreateDirectoryForFileIfNotExists(settingsFile);
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

        internal static async Task<Settings> LoadSettingsJsonAsync<T>()
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
        /// Saves DLSSRecords object to the known position for a dynamic dlss_records.json file.
        /// </summary>
        /// <param name="items"></param>
        /// <returns>true if the object was saved</returns>
        internal static async Task<bool> SaveDLSSRecordsJsonAsync(DLSSRecords items)
        {
            var dlssRecordsFile = Path.Combine(GetDynamicJsonFolder(), "dlss_records.json");
            CreateDirectoryForFileIfNotExists(dlssRecordsFile);
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



        internal static async Task<DLSSRecords> LoadDLSSRecordsJsonAsync()
        {
#if WINDOWS_STORE
            // For Windows Store we skip dynamic json as we only ever have the static.
            var staticDLSSRecordsFile = Path.Combine(GetStaticJsonFolder(), "dlss_records.json");
            if (File.Exists(staticDLSSRecordsFile) == false)
            {
                // Something bad has happened and static dlss_records was not found. So we will default to empty.
                return new DLSSRecords();
            }

            try
            {
                using (var stream = File.Open(staticDLSSRecordsFile, FileMode.Open))
                {
                    return await JsonSerializer.DeserializeAsync<DLSSRecords>(stream);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return new DLSSRecords();
            }

#else
            var dlssRecordsFile = Path.Combine(GetDynamicJsonFolder(), "dlss_records.json");

            // If the dlss records file doesn't exist we try load from our static version.
            if (File.Exists(dlssRecordsFile) == false)
            {
                var staticDLSSRecordsFile = Path.Combine(GetStaticJsonFolder(), "dlss_records.json");
                if (File.Exists(staticDLSSRecordsFile) == false)
                {
                    // Something bad has happened and static dlss_records was not found. So we will default to empty.
                    return new DLSSRecords();
                }

                try
                {
                    using (var stream = File.Open(staticDLSSRecordsFile, FileMode.Open))
                    {
                        return await JsonSerializer.DeserializeAsync<DLSSRecords>(stream);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return new DLSSRecords();
                }
            }

            try
            {
                using (var stream = File.Open(dlssRecordsFile, FileMode.Open))
                {
                    return await JsonSerializer.DeserializeAsync<DLSSRecords>(stream);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return new DLSSRecords();
            }
#endif
        }

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

        internal static async Task<bool> SaveImportedDLSSRecordsJsonAsync(List<DLSSRecord> importedDLSSRecords)
        {
            var importedDLSSRecordsFile = Path.Combine(GetDynamicJsonFolder(), "imported_dlss_records.json");
            CreateDirectoryForFileIfNotExists(importedDLSSRecordsFile);
            try
            {
                using (var stream = File.Open(importedDLSSRecordsFile, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(stream, importedDLSSRecords, jsonSerializerOptions);
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
