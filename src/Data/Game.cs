using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Xaml.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    public abstract partial class Game : ObservableObject, IComparable<Game> //, INotifyPropertyChanged
    {
        [PrimaryKey]
        [Column("id")]
        public string ID { get; set; } = String.Empty;

        [Column("platform_id")]
        public string PlatformId { get; set; } = String.Empty;

        [ObservableProperty]
        [property: Column("title")]
        string title = String.Empty;

        [Column("install_path")]
        public string InstallPath { get; set; }

        [ObservableProperty]
        [property: Column("cover_image")]
        string coverImage = String.Empty;

        [ObservableProperty]
        [property: Column("base_dlss_version")]
        string baseDLSSVersion = String.Empty;

        [ObservableProperty]
        [property: Column("current_dlss_version")]
        string currentDLSSVersion = String.Empty;

        [ObservableProperty]
        [property: Column("current_dlss_hash")]
        string currentDLSSHash = String.Empty;

        [ObservableProperty]
        [property: Column("base_dlss_hash")]
        string baseDLSSHash = String.Empty;

        [ObservableProperty]
        [property: Column("has_dlss")]
        bool hasDLSS = false;

        [ObservableProperty]
        [property: Ignore]
        bool processing = false;

        [Ignore]
        public abstract GameLibrary GameLibrary { get; }

        [Ignore]
        string expectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_600_900.webp");

        protected void SetID()
        {
            ID = GameLibrary switch
            {
                GameLibrary.Steam => $"steam_{PlatformId}",
                GameLibrary.GOG => $"gog_{PlatformId}",
                GameLibrary.EpicGamesStore => $"epicgamesstore_{PlatformId}",
                GameLibrary.UbisoftConnect => $"ubisoftconnect_{PlatformId}",
                GameLibrary.XboxApp => $"xboxapp_{PlatformId}",
                GameLibrary.ManuallyAdded => $"manuallyadded_{PlatformId}",
                _ => throw new Exception($"Unknown GameLibrary {GameLibrary} while setting ID"),
            };
        }

        /// <summary>
        /// Detects DLSS and updates cover image.
        /// </summary>
        public void ProcessGame(bool autoSave = true)
        {
            if (String.IsNullOrEmpty(InstallPath))
            {
                return;
            }

            if (Directory.Exists(InstallPath) == false)
            {
                return;
            }

            Processing = true;

            ThreadPool.QueueUserWorkItem((stateInfo) =>
            {
                LoadCoverImage();

                var enumerationOptions = new EnumerationOptions();
                enumerationOptions.RecurseSubdirectories = true;
                enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
                var dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);

                var hasDLSS = false;
                var currentDLSSVersion = String.Empty;
                var currentDLSSHash = String.Empty;
                var baseDLSSVersion = String.Empty;
                var baseDLSSHash = String.Empty;

                if (dlssDlls.Length > 0)
                {
                    hasDLSS = true;

                    // TODO: Handle a single folder with various versions of DLSS detected.
                    // Currently we are just using the first.

                    foreach (var dlssDll in dlssDlls)
                    {
                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                        currentDLSSVersion = dllVersionInfo.GetFormattedFileVersion();
                        currentDLSSHash = dllVersionInfo.GetMD5Hash();
                        break;
                    }

                    dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", enumerationOptions);
                    if (dlssDlls.Length > 0)
                    {
                        foreach (var dlssDll in dlssDlls)
                        {
                            var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                            baseDLSSVersion = dllVersionInfo.GetFormattedFileVersion();
                            baseDLSSHash = dllVersionInfo.GetMD5Hash();
                            break;
                        }
                    }
                }
                else
                {
                    hasDLSS = false;
                }


                // Now update all the data on the UI therad.
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    HasDLSS = hasDLSS;
                    if (hasDLSS)
                    {
                        CurrentDLSSVersion = currentDLSSVersion;
                        CurrentDLSSHash = currentDLSSHash;
                        BaseDLSSVersion = baseDLSSVersion;
                        BaseDLSSHash = baseDLSSHash;
                    }
                    else
                    {
                        CurrentDLSSVersion = "N/A";
                        CurrentDLSSHash = String.Empty;
                        BaseDLSSVersion = String.Empty;
                        BaseDLSSHash = String.Empty;
                    }

                    if (autoSave)
                    {
                        await SaveToDatabaseAsync();
                    }
                    
                    Processing = false;
                });
            });
        }

        void LoadCoverImage()
        {
            // TODO: Update if the image last write is > 1 week old or something
            if (File.Exists(expectedCoverImage))
            {
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = expectedCoverImage;
                });
            }
            else
            {
                UpdateCacheImage();
            }
        }

        protected abstract void UpdateCacheImage();

        internal (bool Success, string Message, bool PromptToRelaunchAsAdmin) ResetDll()
        {
            var enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
            var foundDllBackups = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", enumerationOptions);
            if (foundDllBackups.Length == 0)
            {
                return (false, "Unable to reset to default. Please repair your game manually.", false);
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(foundDllBackups.First());
            var resetToVersion = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";

            foreach (var dll in foundDllBackups)
            {
                try
                {
                    var dllPath = Path.GetDirectoryName(dll);
                    var targetDllPath = Path.Combine(dllPath, "nvngx_dlss.dll");
                    File.Move(dll, targetDllPath, true);
                }
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsRunningAsAdministrator())
                    {
                        return (false, "Unable to reset to default. Please repair your game manually.", false);
                    }
                    else
                    {
                        return (false, "Unable to reset to default. Running DLSS Swapper as administrator may fix this.", true);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return (false, "Unable to reset to default. Please repair your game manually.", false);
                }
            }

            App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                CurrentDLSSVersion = resetToVersion;
                BaseDLSSVersion = String.Empty;
                await SaveToDatabaseAsync();
            });



            return (true, String.Empty, false);
        }

        /// <summary>
        /// Attempts to update a DLSS dll in a given game.
        /// </summary>
        /// <param name="dlssRecord"></param>
        /// <returns>Tuple containing a boolean of Success, if this is false there will be an error message in the Message response.</returns>
        internal (bool Success, string Message, bool PromptToRelaunchAsAdmin) UpdateDll(DLSSRecord dlssRecord)
        {
            if (dlssRecord == null)
            {
                return (false, "Unable to swap DLSS dll as your DLSS record was not found.", false);
            }
            var enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
            var foundDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);
            if (foundDlls.Length == 0)
            {
                return (false, "Unable to swap DLSS dll as there were no DLSS records to update.", false);
            }

            var tempPath = Storage.GetTemp();
            var tempDllFile = Path.Combine(tempPath, $"nvngx_dlss_{dlssRecord.MD5Hash}", $"nvngx_dlss.dll");
            Storage.CreateDirectoryForFileIfNotExists(tempDllFile);

            using (var archive = ZipFile.OpenRead(dlssRecord.LocalRecord.ExpectedPath))
            {
                var zippedDlls = archive.Entries.Where(x => x.Name.EndsWith(".dll")).ToArray();
                if (zippedDlls.Length == 0)
                {
                    throw new Exception("DLSS zip was invalid (no dll found).");
                }
                else if (zippedDlls.Length > 1)
                {
                    throw new Exception("DLSS zip was invalid (more than one dll found).");
                }

                zippedDlls[0].ExtractToFile(tempDllFile, true);
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(tempDllFile);

            var dlssVersion = versionInfo.GetFormattedFileVersion();
            if (dlssRecord.MD5Hash != versionInfo.GetMD5Hash())
            {
                return (false, "Unable to swap DLSS dll as zip was invalid (dll hash was invalid).", false);
            }

            // Validate new DLL
            if (Settings.Instance.AllowUntrusted == false)
            {
                var isTrusted = WinTrust.VerifyEmbeddedSignature(tempDllFile);
                if (isTrusted == false)
                {
                    return (false, "Unable to swap DLSS dll as we are unable to verify the signature of the DLSS version you are trying to use.\nIf you wish to override this decision please enable 'Allow Untrusted' in settings.", false);
                }
            }

            var baseDllVersion = String.Empty;

            // Backup old dlls.
            foreach (var dll in foundDlls)
            {
                var dllPath = Path.GetDirectoryName(dll);
                var targetDllPath = Path.Combine(dllPath, "nvngx_dlss.dll.dlsss");
                if (File.Exists(targetDllPath) == false)
                {
                    try
                    {
                        var defaultVersionInfo = FileVersionInfo.GetVersionInfo(dll);
                        baseDllVersion = $"{defaultVersionInfo.FileMajorPart}.{defaultVersionInfo.FileMinorPart}.{defaultVersionInfo.FileBuildPart}.{defaultVersionInfo.FilePrivatePart}";

                        File.Copy(dll, targetDllPath, true);
                    }
                    catch (UnauthorizedAccessException err)
                    {
                        Logger.Error($"UnauthorizedAccessException: {err.Message}");
                        if (App.CurrentApp.IsRunningAsAdministrator())
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);

                        }
                        else
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err.Message);
                        return (false, "Unable to swap DLSS dll. Please check your error log for more information.", false);
                    }
                }
            }



            foreach (var dll in foundDlls)
            {
                try
                {
                    File.Copy(tempDllFile, dll, true);
                }
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsRunningAsAdministrator())
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);

                    }
                    else
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return (false, "Unable to swap DLSS dll. Please check your error log for more information.", false);
                }
            }


            App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                CurrentDLSSVersion = dlssRecord.Version;
                if (String.IsNullOrEmpty(baseDllVersion) == false)
                {
                    BaseDLSSVersion = baseDllVersion;
                }
                await SaveToDatabaseAsync();
            });

            try
            {
                File.Delete(tempDllFile);
            }
            catch (Exception err)
            {
                // NOOP
                Logger.Error(err.Message);
            }

            return (true, String.Empty, false);
        }

        #region IComparable<Game>
        public int CompareTo(Game other)
        {
            return Title?.CompareTo(other.Title) ?? -1;
        }
        #endregion

        /*
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        */


        protected void ResizeCover(string imageSource)
        {
            // TODO: 
            // - find optimal format (eg, is displaying 100 webp images more intense than 100 png images)
            // - load image based on scale
            try
            {
                using (var image = SixLabors.ImageSharp.Image.Load(imageSource))
                {
                    // If images are really big we resize to at least 3x the 200x300 we display as.
                    // In future this should be updated to resize to display scale.
                    // If the image is smaller than this we are just saving as png.
                    var resizeOptions = new ResizeOptions()
                    {
                        Size = new Size(200 * 3, 300 * 3),
                        Sampler = KnownResamplers.Lanczos5,
                        Mode = ResizeMode.Min, // If image is smaller it won't be resized up.
                    };
                    image.Mutate(x => x.Resize(resizeOptions));
                    //image.SaveAsPng(expectedCoverImage);
                    image.SaveAsWebp(expectedCoverImage);
                }

                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = expectedCoverImage;
                });
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        protected async void DownloadCover(string url)
        {
            var extension = Path.GetExtension(url);

            // Path.GetExtension retains query arguments, so ths will remove them if they exist.
            if (extension.Contains('?'))
            {
                extension = extension.Substring(0, extension.IndexOf("?"));
            }
            var tempFile = Path.Combine(Storage.GetTemp(), $"{ID}.{extension}");


            try
            {
                using (var fileStream = new FileStream(tempFile, FileMode.Create))
                {
                    var httpResponseMessage = await App.CurrentApp.HttpClient.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    if (httpResponseMessage.IsSuccessStatusCode == false)
                    {
                        return;
                    }

                    // This could be optimised by loading stream directly to ImageSharp and skip
                    // the save/load to disk.
                    using (var stream = httpResponseMessage.Content.ReadAsStream())
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                // Now if the image is downloaded lets resize it, 
                ResizeCover(tempFile);
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
            finally
            {
                // Cleanup temp file.
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        public async Task SaveToDatabaseAsync()
        {
            try
            {
                var rowsChanged = await App.CurrentApp.Database.InsertOrReplaceAsync(this);
                if (rowsChanged == 0)
                {
                    Logger.Error($"Tried to save game to database but rowsChanged was 0.");
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        public async Task DeleteAsync()
        {
            try
            {
                await App.CurrentApp.Database.DeleteAsync(this);

                var thumbnailImages = Directory.GetFiles(Storage.GetImageCachePath(), $"{ID}_*", SearchOption.AllDirectories);
                foreach (var thumbnailImage in thumbnailImages)
                {
                    File.Delete(thumbnailImage);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}
