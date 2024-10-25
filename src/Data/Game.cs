using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.UserControls;
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
using Windows.Storage.Pickers;

namespace DLSS_Swapper.Data
{
    public abstract partial class Game : ObservableObject, IComparable<Game>, IEquatable<Game> //, INotifyPropertyChanged
    {
        [PrimaryKey]
        [Column("id")]
        public string ID { get; set; } = string.Empty;

        [Column("platform_id")]
        public string PlatformId { get; set; } = string.Empty;

        [ObservableProperty]
        [property: Column("title")]
        string title = string.Empty;

        [Column("install_path")]
        public string InstallPath { get; set; } = string.Empty;

        [ObservableProperty]
        [property: Column("cover_image")]
        string coverImage = string.Empty;

        [ObservableProperty]
        [property: Column("base_dlss_version")]
        string baseDLSSVersion = string.Empty;

        [ObservableProperty]
        [property: Column("current_dlss_version")]
        string currentDLSSVersion = string.Empty;

        [ObservableProperty]
        [property: Column("current_dlss_hash")]
        string currentDLSSHash = string.Empty;

        [ObservableProperty]
        [property: Column("base_dlss_hash")]
        string baseDLSSHash = string.Empty;

        [ObservableProperty]
        [property: Column("has_dlss")]
        bool hasDLSS = false;

        [ObservableProperty]
        [property: Column("notes")]
        string notes = string.Empty;

        [ObservableProperty]
        [property: Column("is_favourite")]
        bool isFavourite = false;

        [ObservableProperty]
        [property: Ignore]
        bool processing = false;

        [Ignore]
        public abstract GameLibrary GameLibrary { get; }

        [Ignore]
        public string ExpectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_600_900.webp");
        
        [Ignore]        
        public string ExpectedCustomCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_custom_600_900.webp");

        [Ignore]
        public List<GameAsset> GameAssets { get; } = new List<GameAsset>();

        [Ignore]
        public bool NeedsReload { get; set; } = false;

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
            if (string.IsNullOrEmpty(InstallPath))
            {
                return;
            }

            if (Directory.Exists(InstallPath) == false)
            {
                return;
            }

            Processing = true;

            ThreadPool.QueueUserWorkItem(async (stateInfo) =>
            {
                LoadCoverImage();

                var enumerationOptions = new EnumerationOptions();
                enumerationOptions.RecurseSubdirectories = true;
                enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;

                var oldGameAssets = GameAssets.ToList();
                GameAssets.Clear();
                await App.CurrentApp.Database.ExecuteAsync("DELETE FROM GameAsset WHERE id = ?", ID).ConfigureAwait(false); ;
          
                // TODO: See if changing these to filter specific files, or getting very *.dll and looking for our specific ones is faster

                var dlssDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);
                var dlssgDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlssg.dll", enumerationOptions);
                var dlssdDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlssd.dll", enumerationOptions);
                
                foreach (var dlssDllPath in dlssDllPaths)
                {
                    var gameAsset = new GameAsset()
                    {
                        Id = ID,
                        AssetType = GameAssetType.DLSS,
                        Path = dlssDllPath,
                    };
                    gameAsset.LoadVersionAndHash();
                    GameAssets.Add(gameAsset);

                    var backupGameAsset = gameAsset.GetBackup();
                    if (backupGameAsset is not null)
                    {
                        GameAssets.Add(backupGameAsset);
                    }
                }


                foreach (var dlssDllPath in dlssgDllPaths)
                {
                    var gameAsset = new GameAsset()
                    {
                        Id = ID,
                        AssetType = GameAssetType.DLSS_FG,
                        Path = dlssDllPath,
                    };
                    gameAsset.LoadVersionAndHash();
                    GameAssets.Add(gameAsset);

                    var backupGameAsset = gameAsset.GetBackup();
                    if (backupGameAsset is not null)
                    {
                        GameAssets.Add(backupGameAsset);
                    }
                }


                foreach (var dlssDll in dlssdDllPaths)
                {
                    var gameAsset = new GameAsset()
                    {
                        Id = ID,
                        AssetType = GameAssetType.DLSS_RR,
                        Path = dlssDll,
                    };
                    gameAsset.LoadVersionAndHash();
                    GameAssets.Add(gameAsset);

                    var backupGameAsset = gameAsset.GetBackup();
                    if (backupGameAsset is not null)
                    {
                        GameAssets.Add(backupGameAsset);
                    }
                }

                HasDLSS = false;


                var newCurrentDLSSVersion = string.Empty;
                var newCurrentDLSSHash = string.Empty;
                var newBaseDLSSVersion = string.Empty;
                var newBaseDLSSHash = string.Empty;


                if (GameAssets.Any())
                {
                    await App.CurrentApp.Database.InsertAllAsync(GameAssets).ConfigureAwait(false);

                    var dlssGameAssets = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS).ToList();
                    var dlssgGameAssets = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS_FG).ToList();
                    var dlssdGameAssets = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS_RR).ToList();

                    var dlssGameAssetsBackups = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS_BACKUP).ToList();
                    var dlssgGameAssetsBackups = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS_FG_BACKUP).ToList();
                    var dlssdGameAssetsBackups = GameAssets.Where(d => d.AssetType == GameAssetType.DLSS_RR_BACKUP).ToList();

                    HasDLSS = true; // = dlssGameAssets.Any();

                    if (HasDLSS)
                    {
                        var firstDlss = dlssGameAssets.First();

                    }

                }

                if (autoSave)
                {
                    await SaveToDatabaseAsync();
                }


                // Now update all the data on the UI therad.
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    if (HasDLSS)
                    {
                        CurrentDLSSVersion = newCurrentDLSSVersion;
                        CurrentDLSSHash = newCurrentDLSSHash;
                        BaseDLSSVersion = newBaseDLSSVersion;
                        BaseDLSSHash = newBaseDLSSHash;
                    }
                    else
                    {
                        CurrentDLSSVersion = "N/A";
                        CurrentDLSSHash = string.Empty;
                        BaseDLSSVersion = string.Empty;
                        BaseDLSSHash = string.Empty;
                    }

                    
                    Processing = false;
                });
            });
        }

        void LoadCoverImage()
        {
            // TODO: Update if the image last write is > 1 week old or something

            if (File.Exists(ExpectedCustomCoverImage))
            {
                // If a custom cover exists use it.
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = ExpectedCustomCoverImage;
                });
            }
            else if (File.Exists(ExpectedCoverImage))
            {
                // If a standard cover exists use it.
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = ExpectedCoverImage;
                });
            }
            else
            {
                // If no cover exists use the abstracted method to get the game as expect for this library.
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
                    if (string.IsNullOrEmpty(dllPath))
                    {
                        throw new Exception("dllPath was null or empty.");
                    }
                    var targetDllPath = Path.Combine(dllPath, "nvngx_dlss.dll");
                    File.Move(dll, targetDllPath, true);
                }
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsAdminUser() is false)
                    {
                        return (false, "Unable to reset to default. Running DLSS Swapper as administrator may fix this.", true);
                    }
                    else
                    {
                        return (false, "Unable to reset to default. Please repair your game manually.", false);
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
                BaseDLSSVersion = string.Empty;
                await SaveToDatabaseAsync();
            });



            return (true, string.Empty, false);
        }

        /// <summary>
        /// Attempts to update a DLSS dll in a given game.
        /// </summary>
        /// <param name="dlssRecord"></param>
        /// <returns>Tuple containing a boolean of Success, if this is false there will be an error message in the Message response.</returns>
        internal (bool Success, string Message, bool PromptToRelaunchAsAdmin) UpdateDll(DLSSRecord dlssRecord)
        {
            if (dlssRecord is null)
            {
                return (false, "Unable to swap DLSS dll as your DLSS record was not found.", false);
            }

            if (dlssRecord.LocalRecord is null)
            {
                return (false, "Unable to swap DLSS dll as your local DLSS record was not found.", false);
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

            var baseDllVersion = string.Empty;

            // Backup old dlls.
            foreach (var dll in foundDlls)
            {
                var dllPath = Path.GetDirectoryName(dll);
                if (string.IsNullOrEmpty(dllPath))
                {
                    Logger.Error("dllPath was null or empty.");
                    return (false, "Unable to swap DLSS dll. Please check your error log for more information.", false);
                }

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
                        if (App.CurrentApp.IsAdminUser() is false)
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);

                        }
                        else
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);
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
                    if (App.CurrentApp.IsAdminUser() is false)
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                    }
                    else
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);
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
                if (string.IsNullOrEmpty(baseDllVersion) == false)
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

            return (true, string.Empty, false);
        }

        #region IComparable<Game>
        public int CompareTo(Game? other)
        {
            if (other is null)
            {
                return -1;
            }

            return Title.CompareTo(other.Title);
        }
        #endregion

        /*
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged = null;
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
                    //image.SaveAsPng(ExpectedCoverImage);
                    image.SaveAsWebp(ExpectedCoverImage);
                    //image.SaveAsJpeg(ExpectedCoverImage);
                }

                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = ExpectedCoverImage;
                });
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }


        public void AddCustomCover(string imageSource)
        {
            using (var fileStream = File.OpenRead(imageSource))
            {
                AddCustomCover(fileStream);
            }
        }

        public void AddCustomCover(Stream stream)
        {
            // TODO: 
            // - find optimal format (eg, is displaying 100 webp images more intense than 100 png images)
            // - load image based on scale
            try
            {
                using (var image = SixLabors.ImageSharp.Image.Load(stream))
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
                    //image.SaveAsPng(ExpectedCustomCoverImage);
                    image.SaveAsWebp(ExpectedCustomCoverImage);
                    //image.SaveAsJpeg(ExpectedCustomCoverImage);
                }

                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = string.Empty;
                    CoverImage = ExpectedCustomCoverImage;
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


        public async Task PromptToRemoveCustomCover()
        {
            var dialog = new EasyContentDialog(App.CurrentApp.MainWindow.Content.XamlRoot)
            {
                Title = $"Remove custom cover?",
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = "Are you sure you want to remove the custom cover image?",
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (File.Exists(ExpectedCustomCoverImage))
                {
                    File.Delete(ExpectedCustomCoverImage);
                }

                // Will load default or attempt to fetch fresh.
                LoadCoverImage();
            }
        }

        public async Task PromptToBrowseCustomCover()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
                var fileOpenPicker = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    ViewMode = PickerViewMode.Thumbnail,
                };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".webp");
                WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

                var coverImageFile = await fileOpenPicker.PickSingleFileAsync();

                if (coverImageFile is null)
                {
                    return;
                }

                AddCustomCover(coverImageFile.Path);
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        public bool Equals(Game? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ID == other.ID)
            {
                return true;
            }

            if (PlatformId == other.PlatformId)
            {
                return true;
            }

            return false;
        }

        protected bool ParentUpdateFromGame(Game game)
        {
            var didChange = false;

            if (Title != game.Title)
            {
                Title = game.Title;
                didChange = true;
            }

            if (InstallPath != game.InstallPath)
            {
                InstallPath = game.InstallPath;
                didChange = true;
            }

            if (CoverImage != game.CoverImage)
            {
                CoverImage = game.CoverImage;
                didChange = true;
            }

            if (BaseDLSSVersion != game.BaseDLSSVersion)
            {
                BaseDLSSVersion = game.BaseDLSSVersion;
                didChange = true;
            }

            if (BaseDLSSHash != game.BaseDLSSHash)
            {
                BaseDLSSHash = game.BaseDLSSHash;
                didChange = true;
            }

            if (CurrentDLSSVersion != game.CurrentDLSSVersion)
            {
                CurrentDLSSVersion = game.CurrentDLSSVersion;
                didChange = true;
            }

            if (CurrentDLSSHash != game.CurrentDLSSHash)
            {
                CurrentDLSSHash = game.CurrentDLSSHash;
                didChange = true;
            }

            if (HasDLSS != game.HasDLSS)
            {
                HasDLSS = game.HasDLSS;
                didChange = true;
            }

            // We don't copy across the following properties as it is assume this object has the latest revisions:
            // - Notes
            // - IsFavourite

            return didChange;
        }

        public abstract bool UpdateFromGame(Game game);

        public async Task LoadGameAssetsFromCacheAsync()
        {
            GameAssets.Clear();

            var gameAssets = await App.CurrentApp.Database.Table<GameAsset>().Where(ga => ga.Id == ID).ToListAsync();
            if (gameAssets.Any())
            {
                foreach (var gameAsset in gameAssets)
                {
                     
                }
                // TODO: Check each game asset, if it exists and matches what we expect then we can skip loading it again
                // If it isn't found or doesn't match, need to re-scan.
                GameAssets.AddRange(gameAssets);
            }
        }
    }
}
