using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Helpers;

namespace DLSS_Swapper
{
    static class Storage
    {

        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

#if !WINDOWS_STORE
        // TODO: Test portable app.
#if PORTABLE
        static string unpackagedStoragePath => Path.Combine(Directory.GetCurrentDirectory(), "stored_data");
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


        public static async Task<T> LoadJsonAsync<T>(string filename) where T : class 
        {
#if WINDOWS_STORE
            try
            {
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



        public static async Task<bool> SaveJsonAsync<T>(T obj, string filename) where T : class
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
#else
            return Path.GetTempPath();
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
    }
}
