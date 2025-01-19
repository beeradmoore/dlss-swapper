﻿using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;
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
        string? coverImage = null;

        /*
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
        */

        [ObservableProperty]
        [property: Column("has_swappable_items")]
        bool hasSwappableItems = false;

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
        //public string ExpectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_600_900.jpg");
        //public string ExpectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_600_900.png");
        public string ExpectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_400_600.png");
        //public string ExpectedCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_600_900.webp");

        [Ignore]
        //public string ExpectedCustomCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_custom_600_900.jpg");
        //public string ExpectedCustomCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_custom_600_900.png");
        public string ExpectedCustomCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_custom_400_600.png");
        //public string ExpectedCustomCoverImage => Path.Combine(Storage.GetImageCachePath(), $"{ID}_custom_600_900.webp");

        [Ignore]
        public List<GameAsset> GameAssets { get; } = new List<GameAsset>();

        [Ignore]
        public bool NeedsReload { get; set; } = false;

        bool _isLoadingCoverImage = false;

        [ObservableProperty]
        [property:Ignore]
        GameAsset? currentDLSS = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentDLSS_G = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentDLSS_D = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentFSR_31_DX12 = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentFSR_31_VK = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentXeSS = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentXeLL = null;

        [ObservableProperty]
        [property: Ignore]
        GameAsset? currentXeSS_FG = null;
                

        protected void SetID()
        {
            // Seeing as we use ID, it sure would be a shame if a PlatformId was set to "C:\Program Files\"
            // So try to remove all funky characters before 

            var platformId = PlatformId;
            foreach (var invalidPathChar in PathHelpers.InvalidFileNamePathChars)
            {
                if (platformId.Contains(invalidPathChar))
                {
                    platformId = platformId.Replace(invalidPathChar, '_');
                }
            }

            ID = GameLibrary switch
            {
                GameLibrary.Steam => $"steam_{platformId}",
                GameLibrary.GOG => $"gog_{platformId}",
                GameLibrary.EpicGamesStore => $"epicgamesstore_{platformId}",
                GameLibrary.UbisoftConnect => $"ubisoftconnect_{platformId}",
                GameLibrary.XboxApp => $"xboxapp_{platformId}",
                GameLibrary.ManuallyAdded => $"manuallyadded_{platformId}",
                _ => throw new Exception($"Unknown GameLibrary {GameLibrary} while setting ID"),
            };
        }

        /// <summary>
        /// Detects DLSS and updates cover image.
        /// </summary>
        public void ProcessGame(bool autoSave = true)
        {
            App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                NeedsReload = false;
            });

            if (string.IsNullOrEmpty(InstallPath))
            {
                return;
            }

            if (Directory.Exists(InstallPath) == false)
            {
                return;
            }

            App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                Processing = true;
                HasSwappableItems = false;
            });

            ThreadPool.QueueUserWorkItem(async (stateInfo) =>
            {
                var enumerationOptions = new EnumerationOptions();
                enumerationOptions.RecurseSubdirectories = true;
                enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;

                var oldGameAssets = GameAssets.ToList();
                GameAssets.Clear();
                await App.CurrentApp.Database.ExecuteAsync("DELETE FROM GameAsset WHERE id = ?", ID).ConfigureAwait(false);

                // TODO: See if changing these to filter specific files, or getting very *.dll and looking for our specific ones is faster
                var dllPaths = Directory.GetFiles(InstallPath, "*.dll", enumerationOptions);

                /*
                var dlssDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);
                var dlssgDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlssg.dll", enumerationOptions);
                var dlssdDllPaths = Directory.GetFiles(InstallPath, "nvngx_dlssd.dll", enumerationOptions);
                var xessDllPaths = Directory.GetFiles(InstallPath, "libxess.dll", enumerationOptions);
                */

                foreach (var dllPath in dllPaths)
                {
                    var dllName = Path.GetFileName(dllPath);

                    // The case of these files should never change, right?
                    if (dllName == "nvngx_dlss.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.DLSS,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "nvngx_dlssg.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.DLSS_G,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "nvngx_dlssd.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.DLSS_D,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "amd_fidelityfx_dx12.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.FSR_31_DX12,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "amd_fidelityfx_vk.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.FSR_31_VK,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "libxess.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.XeSS,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "libxell.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.XeLL,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }
                    else if (dllName == "libxess_fg.dll")
                    {
                        var gameAsset = new GameAsset()
                        {
                            Id = ID,
                            AssetType = GameAssetType.XeSS_FG,
                            Path = dllPath,
                        };
                        gameAsset.LoadVersionAndHash();
                        GameAssets.Add(gameAsset);

                        LoadBackupForGameAsset(gameAsset);
                    }

                    /*
                    var backupGameAsset = gameAsset.GetBackup();
                    if (backupGameAsset is not null)
                    {
                        GameAssets.Add(backupGameAsset);
                    }
                    */
                }


                var newHasSwappableItems = false;

                if (GameAssets.Any())
                {
                    newHasSwappableItems = true;

                    //App.CurrentApp.Database.ExecuteAsync
                    //savePoint is not valid, and should be the result of a call to SaveTransactionPoint.
                    await App.CurrentApp.Database.InsertAllAsync(GameAssets, false).ConfigureAwait(false);                  
                }



                // Now update all the data on the UI therad.
                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    HasSwappableItems = newHasSwappableItems;

                    if (autoSave)
                    {
                        await SaveToDatabaseAsync();
                    }

                    Processing = false;
                });
            });
        }

        void LoadBackupForGameAsset(GameAsset gameAsset)
        {
            var backupPath = $"{gameAsset.Path}.dlsss";
            if (File.Exists(backupPath))
            {
                var gameAssetBackup = new GameAsset()
                {
                    Id = ID,
                    AssetType = DLLManager.Instance.GetAssetBackupType(gameAsset.AssetType),
                    Path = backupPath,
                };
                gameAssetBackup.LoadVersionAndHash();
                GameAssets.Add(gameAssetBackup);
            }
        }


        public async Task LoadCoverImageAsync()
        {
            if (_isLoadingCoverImage == true)
            {
                return;
            }

            _isLoadingCoverImage = true;

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
                await UpdateCacheImageAsync();
            }

            _isLoadingCoverImage = false;
        }

        protected abstract Task UpdateCacheImageAsync();

        internal async Task<(bool Success, string Message, bool PromptToRelaunchAsAdmin)> ResetDllAsync(GameAssetType gameAssetType)
        {
            var backupRecordType = DLLManager.Instance.GetAssetBackupType(gameAssetType);
            var existingBackupRecords = this.GameAssets.Where(x => x.AssetType == backupRecordType).ToList();

            if (existingBackupRecords.Count == 0)
            {
                Logger.Info("No backup records found.");
                return (false, "Unable to reset to default. Please repair your game manually.", false);
            }
            else if (existingBackupRecords.Count == 1)
            {
                var existingBackupRecord = existingBackupRecords[0];

                var primaryRecordName = existingBackupRecord.Path.Replace(".dlsss", string.Empty);
                var existingRecords = this.GameAssets.Where(x => x.AssetType == gameAssetType && x.Path.Equals(primaryRecordName)).ToList();

                if (existingRecords.Count != 1)
                {
                    Logger.Info("Backup record was found, existing records were not.");
                    return (false, "Unable to reset to default. Please repair your game manually.", false);
                }

                var existingRecord = existingRecords[0];

                try
                {
                    File.Move(existingBackupRecord.Path, existingRecord.Path, true);
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

                var newGameAsset = new GameAsset()
                {
                    Id = ID,
                    AssetType = gameAssetType,
                    Path = existingRecord.Path,
                    Version = existingBackupRecord.Version,
                    Hash = existingBackupRecord.Hash,
                };

                UpdateCurrentAsset(newGameAsset, gameAssetType);

                GameAssets.Remove(existingRecord);
                GameAssets.Remove(existingBackupRecord);
                GameAssets.Add(newGameAsset);

                // Update game assets list by deleting and re-adding.
                await App.CurrentApp.Database.ExecuteAsync("DELETE FROM GameAsset WHERE id = ?", ID).ConfigureAwait(false);
                await App.CurrentApp.Database.InsertAllAsync(GameAssets, false).ConfigureAwait(false);

                return (true, string.Empty, false);
            }
            else
            {
                Logger.Error("Expected 1 backup records. Founds more.");
                return (false, "Unable to reset to default. Please repair your game manually.", false);
            }
        }

        /// <summary>
        /// Attempts to update a DLSS dll in a given game.
        /// </summary>
        /// <param name="dlssRecord"></param>
        /// <returns>Tuple containing a boolean of Success, if this is false there will be an error message in the Message response.</returns>
        internal async Task<(bool Success, string Message, bool PromptToRelaunchAsAdmin)> UpdateDllAsync(DLLRecord dllRecord)
        {
            if (dllRecord is null)
            {
                return (false, "Unable to swap dll as your dll record was not found.", false);
            }

            if (dllRecord.LocalRecord is null)
            {
                return (false, "Unable to swap dll as your local dll record was not found.", false);
            }

            if (File.Exists(dllRecord.LocalRecord.ExpectedPath) == false)
            {
                return (false, "Downloaded zip not found.", false);
            }

            var existingRecords = this.GameAssets.Where(x => x.AssetType == dllRecord.AssetType).ToList();
            if (existingRecords.Count == 0)
            {
                return (false, "Unable to swap dll as there were no dll records to update.", false);
            }

            var backupRecordType = DLLManager.Instance.GetAssetBackupType(dllRecord.AssetType);
            var existingBackupRecords = this.GameAssets.Where(x => x.AssetType == backupRecordType).ToList();
            
            // TODO: Handle more than the first record.
            var currentRecord = existingRecords[0];

            var recordFileName = Path.GetFileName(currentRecord.Path);
            var recordFileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentRecord.Path);

            var tempPath = Storage.GetTemp();
            var tempDllFile = Path.Combine(tempPath, $"{recordFileNameWithoutExtension}_{dllRecord.MD5Hash}", recordFileName);
            Storage.CreateDirectoryForFileIfNotExists(tempDllFile);


            using (var archive = ZipFile.OpenRead(dllRecord.LocalRecord.ExpectedPath))
            {
                var zippedDlls = archive.Entries.Where(x => x.Name == recordFileName).ToArray();
                if (zippedDlls.Length == 0)
                {
                    throw new Exception("Dll zip was invalid (no dll found).");
                }
                else if (zippedDlls.Length > 1)
                {
                    throw new Exception("Dll zip was invalid (more than one dll found).");
                }

                zippedDlls[0].ExtractToFile(tempDllFile, true);
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(tempDllFile);
            var dllVersion = versionInfo.GetFormattedFileVersion();
            if (dllRecord.MD5Hash != versionInfo.GetMD5Hash())
            {
                return (false, "Unable to swap dll as zip was invalid (dll hash was invalid).", false);
            }


            // Validate new DLL
            if (Settings.Instance.AllowUntrusted == false)
            {
                var isTrusted = WinTrust.VerifyEmbeddedSignature(tempDllFile);
                if (isTrusted == false)
                {
                    return (false, "Unable to swap dll as we are unable to verify the signature of the version you are trying to use.\nIf you wish to override this decision please enable 'Allow Untrusted' in settings.", false);
                }
            }

            var newGameAssets = new List<GameAsset>();

            if (existingBackupRecords.Count == 0)
            {
                // Backup old dlls if no backup exists.
                foreach (var existingRecord in existingRecords)
                {
                    var dllPath = Path.GetDirectoryName(existingRecord.Path);
                    if (string.IsNullOrEmpty(dllPath))
                    {
                        Logger.Error("dllPath was null or empty.");
                        return (false, "Unable to swap dll. Please check your error log for more information.", false);
                    }

                    // Ensure we don't do anything if the target exists.
                    var backupDllPath = Path.Combine(dllPath, $"{recordFileName}.dlsss");
                    if (File.Exists(backupDllPath) == false)
                    {
                        try
                        {
                            var defaultVersionInfo = FileVersionInfo.GetVersionInfo(existingRecord.Path);
                            File.Copy(existingRecord.Path, backupDllPath);

                            var backupGameAsset = new GameAsset()
                            {
                                Id = ID,
                                AssetType = backupRecordType,
                                Path = backupDllPath,
                                Version = existingRecord.Version,
                                Hash = existingRecord.Hash,
                            };
                            newGameAssets.Add(backupGameAsset);
                        }
                        catch (UnauthorizedAccessException err)
                        {
                            Logger.Error($"UnauthorizedAccessException: {err.Message}");
                            if (App.CurrentApp.IsAdminUser() is false)
                            {
                                return (false, "Unable to swap dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);

                            }
                            else
                            {
                                return (false, "Unable to swap dll as we are unable to write to the target directory.", false);
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err.Message);
                            return (false, "Unable to swap dll. Please check your error log for more information.", false);
                        }
                    }
                }
            }

            foreach (var existingRecord in existingRecords)
            {
                try
                {
                    File.Copy(tempDllFile, existingRecord.Path, true);
                    
                    var newGameAsset = new GameAsset()
                    {
                        Id = ID,
                        AssetType = dllRecord.AssetType,
                        Path = existingRecord.Path,
                        Version = dllVersion,
                        Hash = dllRecord.MD5Hash,
                    };
                    // No need to call LoadVersionAndHash, the data is already here.
                    newGameAssets.Add(newGameAsset);
                }
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsAdminUser() is false)
                    {
                        return (false, "Unable to swap dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                    }
                    else
                    {
                        return (false, "Unable to DLSS dll as we are unable to write to the target directory.", false);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return (false, "Unable to swap dll. Please check your error log for more information.", false);
                }
            }

            foreach (var existingRecrod in existingRecords)
            {
                var didRemove = GameAssets.Remove(existingRecrod);
            }
            GameAssets.AddRange(newGameAssets);

            // This should never be null.
            var firstNewGameAsset = newGameAssets.FirstOrDefault(x => x.AssetType == dllRecord.AssetType);
            if (firstNewGameAsset is not null)
            {
                UpdateCurrentAsset(firstNewGameAsset, dllRecord.AssetType);
            }

            // Update game assets list by deleting and re-adding.
            await App.CurrentApp.Database.ExecuteAsync("DELETE FROM GameAsset WHERE id = ?", ID).ConfigureAwait(false);
            await App.CurrentApp.Database.InsertAllAsync(GameAssets, false).ConfigureAwait(false);


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

        void UpdateCurrentAsset(GameAsset newGameAsset, GameAssetType gameAssetType)
        {
            App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                if (gameAssetType == GameAssetType.DLSS)
                {
                    CurrentDLSS = null;
                    CurrentDLSS = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.DLSS_G)
                {
                    CurrentDLSS_G = null;
                    CurrentDLSS_G = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.DLSS_D)
                {
                    CurrentDLSS_D = null;
                    CurrentDLSS_D = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.FSR_31_DX12)
                {
                    CurrentFSR_31_DX12 = null;
                    CurrentFSR_31_DX12 = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.FSR_31_VK)
                {
                    CurrentFSR_31_VK = null;
                    CurrentFSR_31_VK = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.XeSS)
                {
                    CurrentXeSS = null;
                    CurrentXeSS = newGameAsset;
                }
                else if (gameAssetType == GameAssetType.XeLL)
                {
                    CurrentXeLL = null;
                    CurrentXeLL = newGameAsset;
                }
                else if (gameAssetType  == GameAssetType.XeSS_FG)
                {
                    CurrentXeSS_FG = null;
                    CurrentXeSS_FG = newGameAsset;
                }
                else
                {
                    Logger.Error($"Unknown AssetType: {gameAssetType}");
                }
            });
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


        protected async Task ResizeCoverAsync(string imageSource)
        {
            // TODO: 
            // - find optimal format (eg, is displaying 100 webp images more intense than 100 png images)
            // - load image based on scale
            try
            {
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync(imageSource))
                {
                    // If images are really big we resize to at least 2x the 200x300 we display as.
                    // In future this should be updated to resize to display scale.
                    // If the image is smaller than this we are just saving as png.
                    var resizeOptions = new ResizeOptions()
                    {
                        Size = new Size(200 * 2, 300 * 2),
                        Sampler = KnownResamplers.Lanczos5,
                        Mode = ResizeMode.Min, // If image is smaller it won't be resized up.
                    };
                    image.Mutate(x => x.Resize(resizeOptions));
                    image.SaveAsPng(ExpectedCoverImage);
                    //image.SaveAsWebp(ExpectedCoverImage);
                    //image.SaveAsJpeg(ExpectedCoverImage);
                }

                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = null;
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
                    image.SaveAsPng(ExpectedCustomCoverImage);
                    //image.SaveAsWebp(ExpectedCustomCoverImage);
                    //image.SaveAsJpeg(ExpectedCustomCoverImage);
                }

                App.CurrentApp.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CoverImage = ExpectedCustomCoverImage;
                });
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        protected async Task DownloadCoverAsync(string url)
        {
            var extension = Path.GetExtension(url);

            // Path.GetExtension retains query arguments, so ths will remove them if they exist.
            if (extension.Contains('?'))
            {
                extension = extension.Substring(0, extension.IndexOf("?"));
            }
            var tempFile = Path.Combine(Storage.GetTemp(), $"{ID}{extension}");


            try
            {
                using (var fileStream = new FileStream(tempFile, FileMode.Create))
                {
                    var httpResponseMessage = await App.CurrentApp.HttpClient.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    if (httpResponseMessage.IsSuccessStatusCode == false)
                    {
                        return;
                    }

                    // This could be optimised by loading stream directly to ImageSharp and skip
                    // the save/load to disk.
                    using (var stream = httpResponseMessage.Content.ReadAsStream())
                    {
                        await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }

                // Now if the image is downloaded lets resize it, 
                await ResizeCoverAsync(tempFile).ConfigureAwait(false);
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                Debugger.Break();
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
                    // TODO: Fix why this happens occasionally to reandom games.
                    // This appears to change to different games in different libraries.
                    Logger.Error($"Tried to save game to database but rowsChanged was 0.");
                    //Debugger.Break();
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                Debugger.Break();
            }
        }

        public async Task DeleteAsync()
        {
            try
            {
                // Sometimes when a game is uninstalled the backup files are not removed, so ensure they are.
                // https://github.com/beeradmoore/dlss-swapper/issues/236

                var gameAssets = await App.CurrentApp.Database.Table<GameAsset>().Where(ga => ga.Id == ID).ToListAsync();
                foreach (var cachedGameAsset in gameAssets)
                {
                    // If its a file we made we should attempt to delete it.
                    if (cachedGameAsset.AssetType == GameAssetType.DLSS_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.DLSS_G_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.DLSS_D_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.FSR_31_DX12_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.FSR_31_VK_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.XeSS_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.XeSS_FG_BACKUP ||
                        cachedGameAsset.AssetType == GameAssetType.XeLL_BACKUP)
                    {
                        if (File.Exists(cachedGameAsset.Path))
                        {
                            Logger.Info($"Deleting {cachedGameAsset.Path}");
                            try
                            {
                                File.Delete(cachedGameAsset.Path);
                            }
                            catch (Exception err)
                            {
                                Logger.Error($"Could not delete {cachedGameAsset.Path}, {err.Message}");
                            }
                        }
                    }
                }
                await App.CurrentApp.Database.Table<GameAsset>().DeleteAsync(ga => ga.Id == ID).ConfigureAwait(false);

               
                // Delete the thumbnails.
                var thumbnailImages = Directory.GetFiles(Storage.GetImageCachePath(), $"{ID}_*", SearchOption.AllDirectories);
                foreach (var thumbnailImage in thumbnailImages)
                {
                    try
                    {
                        Logger.Info($"Deleting {thumbnailImage}");
                        File.Delete(thumbnailImage);
                    }
                    catch (Exception err)
                    {
                        Logger.Error($"Could not delete {thumbnailImage}, {err.Message}");
                    }
                }

                // Delete the game itself.
                await App.CurrentApp.Database.DeleteAsync(this).ConfigureAwait(false);

                // Remove the game from the list.
                GameManager.Instance.RemoveGame(this);
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
                await LoadCoverImageAsync();
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
                InstallPath = PathHelpers.NormalizePath(game.InstallPath);
                didChange = true;
            }

            if (CoverImage != game.CoverImage)
            {
                CoverImage = game.CoverImage;
                didChange = true;
            }

            if (HasSwappableItems != game.HasSwappableItems)
            {
                HasSwappableItems = game.HasSwappableItems;
                didChange = true;
            }

            if (CurrentDLSS != game.CurrentDLSS)
            {
                CurrentDLSS = game.CurrentDLSS;
                didChange = true;
            }

            if (CurrentDLSS_G != game.CurrentDLSS_G)
            {
                CurrentDLSS_G = game.CurrentDLSS_G;
                didChange = true;
            }

            if (CurrentDLSS_D != game.CurrentDLSS_D)
            {
                CurrentDLSS_D = game.CurrentDLSS_D;
                didChange = true;
            }

            if (CurrentFSR_31_DX12 != game.CurrentFSR_31_DX12)
            {
                CurrentFSR_31_DX12 = game.CurrentFSR_31_DX12;
                didChange = true;
            }

            if (CurrentFSR_31_VK != game.CurrentFSR_31_VK)
            {
                CurrentFSR_31_VK = game.CurrentFSR_31_VK;
                didChange = true;
            }

            if (CurrentXeSS != game.CurrentXeSS)
            {
                CurrentXeSS = game.CurrentXeSS;
                didChange = true;
            }

            if (CurrentXeLL != game.CurrentXeLL)
            {
                CurrentXeLL = game.CurrentXeLL;
                didChange = true;
            }

            if (CurrentXeSS_FG != game.CurrentXeSS_FG)
            {
                CurrentXeSS_FG = game.CurrentXeSS_FG;
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
            await LoadCoverImageAsync();

            GameAssets.Clear();
            var gameAssets = await App.CurrentApp.Database.Table<GameAsset>().Where(ga => ga.Id == ID).ToListAsync();
            GameAssets.AddRange(gameAssets);

            foreach (var gameAsset in gameAssets)
            {
                if (gameAsset.AssetType == GameAssetType.DLSS)
                {
                    CurrentDLSS = gameAsset;
                }
                else if(gameAsset.AssetType == GameAssetType.DLSS_G)
                {
                    CurrentDLSS_G = gameAsset;
                }
                else if(gameAsset.AssetType == GameAssetType.DLSS_D)
                {
                    CurrentDLSS_D = gameAsset;
                }
                else if(gameAsset.AssetType == GameAssetType.FSR_31_DX12)
                {
                    CurrentFSR_31_DX12 = gameAsset;
                }
                else if(gameAsset.AssetType == GameAssetType.FSR_31_VK)
                {
                    CurrentFSR_31_VK = gameAsset;
                }
                else if (gameAsset.AssetType == GameAssetType.XeSS)
                {
                    CurrentXeSS = gameAsset;
                }
                else if (gameAsset.AssetType == GameAssetType.XeLL)
                {
                    CurrentXeLL = gameAsset;
                }
                else if (gameAsset.AssetType == GameAssetType.XeSS_FG)
                {
                    CurrentXeSS_FG = gameAsset;
                }
            }
            
            if (gameAssets.Any())
            {
                foreach (var gameAsset in gameAssets)
                {
                    // Check that each of the game assets exist, after we will check if they are what we expect them to be
                    if (File.Exists(gameAsset.Path) == false)
                    {
                        NeedsReload = true;
                        Processing = true;
                        break;
                    }                     
                }

                if (NeedsReload == false)
                {
                    var firstDlssVersion = gameAssets.FirstOrDefault(x => x.AssetType == GameAssetType.DLSS);
                    if (firstDlssVersion is null)
                    {
                        NeedsReload = true;
                        Processing = true;
                    }
                    else
                    {
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(firstDlssVersion.Path);
                        var freshVersion = fileVersionInfo.GetFormattedFileVersion();

                        // TODO: FIX
                        /*
                        if (CurrentDLSSVersion != freshVersion)
                        {
                            NeedsReload = true;
                            Processing = true;
                        }
                        else
                        {
                            // NO-OP, last used DLSS is the same version that we last saw, it is also still on the disk as we expected.
                            NeedsReload = false;
                            Processing = false;
                        }
                        */
        }
    }
            }
            else
            {
                // If there is no known current DLSS version we want to do a full reload incase the game got updated.
                NeedsReload = true;
                return;
            }
        }
    }
}
