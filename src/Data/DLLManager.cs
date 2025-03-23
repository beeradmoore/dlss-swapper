using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;

namespace DLSS_Swapper.Data;

internal class DLLManager
{
    public static DLLManager Instance { get; private set; } = new DLLManager();

    public ObservableCollection<DLLRecord> DLSSRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DLSSGRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DLSSDRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FSR31DX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FSR31VKRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeSSRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeLLRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeSSFGRecords { get; } = new ObservableCollection<DLLRecord>();

    public KnownDLLs KnownDLLs { get; private set; } = new KnownDLLs();

    readonly ReaderWriterLockSlim _knownDLLsReadWriterLock = new ReaderWriterLockSlim();

    internal Manifest? ImportedManifest { get; private set; } = null;

    // Previously: UpdateDLSSRecordsList
    internal void UpdateDLLRecordLists(Manifest manifest)
    {
        _knownDLLsReadWriterLock.EnterWriteLock();
        try
        {
            KnownDLLs = manifest.KnownDLLs;
        }
        finally
        {
            _knownDLLsReadWriterLock.ExitWriteLock();
        }


        // TODO: Only change changed items

        manifest.DLSS.Sort();
        DLSSRecords.Clear();
        foreach (var dllRecord in manifest.DLSS)
        {
            dllRecord.AssetType = GameAssetType.DLSS;
            DLSSRecords.Add(dllRecord);
        }



        manifest.DLSS_G.Sort();
        DLSSGRecords.Clear();
        foreach (var dllRecord in manifest.DLSS_G)
        {
            dllRecord.AssetType = GameAssetType.DLSS_G;
            DLSSGRecords.Add(dllRecord);
        }


        manifest.DLSS_D.Sort();
        DLSSDRecords.Clear();
        foreach (var dllRecord in manifest.DLSS_D)
        {
            dllRecord.AssetType = GameAssetType.DLSS_D;
            DLSSDRecords.Add(dllRecord);
        }


        manifest.FSR_31_DX12.Sort();
        FSR31DX12Records.Clear();
        foreach (var dllRecord in manifest.FSR_31_DX12)
        {
            dllRecord.AssetType = GameAssetType.FSR_31_DX12;
            FSR31DX12Records.Add(dllRecord);
        }


        manifest.FSR_31_VK.Sort();
        FSR31VKRecords.Clear();
        foreach (var dllRecord in manifest.FSR_31_VK)
        {
            dllRecord.AssetType = GameAssetType.FSR_31_VK;
            FSR31VKRecords.Add(dllRecord);
        }


        manifest.XeSS.Sort();
        XeSSRecords.Clear();
        foreach (var dllRecord in manifest.XeSS)
        {
            dllRecord.AssetType = GameAssetType.XeSS;
            XeSSRecords.Add(dllRecord);
        }

        manifest.XeLL.Sort();
        XeLLRecords.Clear();
        foreach (var dllRecord in manifest.XeLL)
        {
            dllRecord.AssetType = GameAssetType.XeLL;
            XeLLRecords.Add(dllRecord);
        }


        manifest.XeSS_FG.Sort();
        XeSSFGRecords.Clear();
        foreach (var dllRecord in manifest.XeSS_FG)
        {
            dllRecord.AssetType = GameAssetType.XeSS_FG;
            XeSSFGRecords.Add(dllRecord);
        }
    }

    /// <summary>
    /// This method should only ever be called once 
    /// </summary>
    /// <param name="manifest"></param>
    internal async Task LoadImportedManifestAsync()
    {
        // This method should only ever be called once.
        // So if you got here, something went wrong.
        if (ImportedManifest is not null)
        {
            Debugger.Break();
            return;
        }

        Manifest? importedManifest = null;
        var importedDLSSRecordsFile = Storage.GetImportedManifestPath();
        if (File.Exists(importedDLSSRecordsFile) == true)
        {
            try
            {
                using (var stream = File.Open(importedDLSSRecordsFile, FileMode.Open))
                {
                    importedManifest = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Manifest).ConfigureAwait(false);
                    if (importedManifest is not null)
                    {
                        ImportedManifest = importedManifest;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }
        else
        {
            // If there is no manifest file it is ok to continue with a blank one.
            ImportedManifest = new Manifest();
        }
    }

    internal static async Task<bool> SaveImportedManifestJsonAsync()
    {
        var importedManifestFile = Storage.GetImportedManifestPath();
        try
        {
            using (var stream = File.Open(importedManifestFile, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(stream, DLLManager.Instance.ImportedManifest, SourceGenerationContext.Default.Manifest);
            }
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return false;
        }
    }

    internal void LoadLocalRecords()
    {
        var allLocalRecords = new ObservableCollection<DLLRecord>[]
        {
            DLSSRecords,
            DLSSDRecords,
            DLSSGRecords,
            FSR31DX12Records,
            FSR31VKRecords,
            XeSSRecords,
            XeLLRecords,
            XeSSFGRecords
        };

        foreach (var recordList in allLocalRecords)
        {
            foreach (var dllRecord in recordList)
            {
                LoadLocalRecordFromDLSSRecord(dllRecord);
            }
        }

        // TODO: Handle imported records
        /*
        foreach (var dlssRecord in ImportedDLSSRecords)
        {
            LoadLocalRecordFromDLSSRecord(dlssRecord, true);
        }
        */
    }

    internal string GetExpectedZipPath(DLLRecord dllRecord, bool isImportedRecord = false)
    {
        var recordType = dllRecord.GetRecordSimpleType();

        if (recordType == string.Empty)
        {
            return string.Empty;
        }

        var zipPath = Path.Combine(Storage.GetStorageFolder(), (isImportedRecord ? $"imported_{recordType}_zip" : $"{recordType}_zip"));

        return zipPath;
    }

    internal void LoadLocalRecordFromDLSSRecord(DLLRecord dllRecord, bool isImportedRecord = false)
    {
        var zipPath = GetExpectedZipPath(dllRecord, isImportedRecord);
        if (string.IsNullOrEmpty(zipPath))
        {
            return;
        }

        var expectedPath = Path.Combine(zipPath, dllRecord.GetExpectedZipName());

        // Expected path was moved in v1.1.7. This is to migrate the zips from old to new path.
        var legacyExpectedPath = Path.Combine(zipPath, $"{dllRecord.Version}_{dllRecord.MD5Hash}.zip");
        if (File.Exists(legacyExpectedPath) == true && File.Exists(expectedPath) == false)
        {
            File.Move(legacyExpectedPath, expectedPath);
        }

        // Load record.
        var localRecord = LocalRecord.FromExpectedPath(expectedPath, isImportedRecord);

        if (isImportedRecord)
        {
            localRecord.IsImported = true;
            localRecord.IsDownloaded = true;
        }

        // If the record exists we will update existing properties, if not we add it as new property.
        if (dllRecord.LocalRecord is null)
        {
            dllRecord.LocalRecord = localRecord;
        }
        else
        {
            dllRecord.LocalRecord.UpdateFromNewLocalRecord(localRecord);
        }
    }

    public string GetAssetTypeName(GameAssetType assetType)
    {
        return assetType switch
        {
            GameAssetType.DLSS => "DLSS",
            GameAssetType.DLSS_G => "DLSS Frame Generation",
            GameAssetType.DLSS_D => "DLSS Ray Reconstruction",
            GameAssetType.FSR_31_DX12 => "FSR 3.1 DirectX 12",
            GameAssetType.FSR_31_VK => "FSR 3.1 Vulkan",
            GameAssetType.XeSS => "XeSS",
            GameAssetType.XeLL => "XeLL",
            GameAssetType.XeSS_FG => "XeSS Frame Generation",
            _ => throw new Exception($"Unknown AssetType: {assetType}"),
        };
    }


    public GameAssetType GetAssetBackupType(GameAssetType assetType)
    {
        return assetType switch
        {
            GameAssetType.DLSS => GameAssetType.DLSS_BACKUP,
            GameAssetType.DLSS_G => GameAssetType.DLSS_G_BACKUP,
            GameAssetType.DLSS_D => GameAssetType.DLSS_D_BACKUP,
            GameAssetType.FSR_31_DX12 => GameAssetType.FSR_31_DX12_BACKUP,
            GameAssetType.FSR_31_VK => GameAssetType.FSR_31_VK_BACKUP,
            GameAssetType.XeSS => GameAssetType.XeSS_BACKUP,
            GameAssetType.XeLL => GameAssetType.XeLL_BACKUP,
            GameAssetType.XeSS_FG => GameAssetType.XeSS_FG_BACKUP,
            _ => throw new Exception($"Unknown AssetType: {assetType}"),
        };
    }

    /// <summary>
    /// Checks to see if the current GameAsset DLL is known to already existing DLL record known GameAsset for a game in a particular library
    /// </summary>
    /// <param name="gameAsset"></param>
    /// <param name="game"></param>
    /// <returns></returns>
    ///
    public bool IsInKnownGameAsset(GameAsset gameAsset, Game game)
    {
        // For each asset type first check if is in the DLSS Swapper manifest
        if (gameAsset.AssetType == GameAssetType.DLSS || gameAsset.AssetType == GameAssetType.DLSS_BACKUP)
        {
            if (DLSSRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.DLSS.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.DLSS_D || gameAsset.AssetType == GameAssetType.DLSS_D_BACKUP)
        {
            if (DLSSDRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.DLSS_D.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.DLSS_G || gameAsset.AssetType == GameAssetType.DLSS_G_BACKUP)
        {
            if (DLSSGRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.DLSS_G.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.FSR_31_DX12 || gameAsset.AssetType == GameAssetType.FSR_31_DX12_BACKUP)
        {
            if (FSR31DX12Records.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.FSR_31_DX12.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.FSR_31_VK || gameAsset.AssetType == GameAssetType.FSR_31_VK_BACKUP)
        {
            if (FSR31VKRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.FSR_31_VK.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.XeSS || gameAsset.AssetType == GameAssetType.XeSS_BACKUP)
        {
            if (XeSSRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.XeSS.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.XeLL || gameAsset.AssetType == GameAssetType.XeLL_BACKUP)
        {
            if (XeLLRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.XeLL.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }
        else if (gameAsset.AssetType == GameAssetType.XeSS_FG || gameAsset.AssetType == GameAssetType.XeSS_FG_BACKUP)
        {
            if (XeSSFGRecords.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            HashedKnownDLL? hashedKnownDLL = null;
            _knownDLLsReadWriterLock.EnterReadLock();
            try
            {
                hashedKnownDLL = KnownDLLs.XeSS_FG.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                _knownDLLsReadWriterLock.ExitReadLock();
            }

            if (hashedKnownDLL is null)
            {
                return false;
            }

            if (hashedKnownDLL.Sources.TryGetValue(game.GameLibrary.ToString(), out var gameHashes) == true)
            {
                if (gameHashes.Contains(game.TitleBase64) == true)
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    internal DLLImportResult ImportDll(string filePath, string? zippedDllFullName = null)
    {
        if (ImportedManifest is null)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, "Import feature is disabled, import manifest could not be loaded.");
        }

        var fileName = Path.GetFileName(filePath);

        ObservableCollection<DLLRecord>? recordList = null;
        List<DLLRecord>? importedRecordList = null;
        GameAssetType? gameAssetType = null;

        if (fileName == "nvngx_dlss.dll")
        {
            gameAssetType = GameAssetType.DLSS;
            recordList = DLSSRecords;
            importedRecordList = ImportedManifest.DLSS;
        }
        else if (fileName == "nvngx_dlssg.dll")
        {
            gameAssetType = GameAssetType.DLSS_G;
            recordList = DLSSGRecords;
            importedRecordList = ImportedManifest.DLSS_G;
        }
        else if (fileName == "nvngx_dlssd.dll")
        {
            gameAssetType = GameAssetType.DLSS_D;
            recordList = DLSSDRecords;
            importedRecordList = ImportedManifest.DLSS_D;
        }
        else if (fileName == "amd_fidelityfx_dx12.dll")
        {
            gameAssetType = GameAssetType.FSR_31_DX12;
            recordList = FSR31DX12Records;
            importedRecordList = ImportedManifest.FSR_31_DX12;
        }
        else if (fileName == "amd_fidelityfx_vk.dll")
        {
            gameAssetType = GameAssetType.FSR_31_VK;
            recordList = FSR31VKRecords;
            importedRecordList = ImportedManifest.FSR_31_VK;
        }
        else if (fileName == "libxess.dll")
        {
            gameAssetType = GameAssetType.XeSS;
            recordList = XeSSRecords;
            importedRecordList = ImportedManifest.XeSS;
        }
        else if (fileName == "libxell.dll")
        {
            gameAssetType = GameAssetType.XeLL;
            recordList = XeLLRecords;
            importedRecordList = ImportedManifest.XeLL;
        }
        else if (fileName == "libxess_fg.dll")
        {
            gameAssetType = GameAssetType.XeSS_FG;
            recordList = XeSSFGRecords;
            importedRecordList = ImportedManifest.XeSS_FG;
        }

        if (gameAssetType is null || recordList is null || importedRecordList is null)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, $"DLL not a known type.");
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        var isTrusted = WinTrust.VerifyEmbeddedSignature(filePath);

        // Don't do anything with untrusted dlls.
        if (Settings.Instance.AllowUntrusted == false && isTrusted == false)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, $"DLL is not trusted by Windows.");
        }

        var dllHash = versionInfo.GetMD5Hash();

        var importingAsDownloadedDll = false;

        // We only need to check recordList and not importedRecordList as imported DLLs are in both lists.
        var existingDll = recordList.FirstOrDefault(x => string.Equals(x.MD5Hash, dllHash, StringComparison.InvariantCultureIgnoreCase));
        if (existingDll is not null)
        {
            // If the DLL is already imported we can skip it.
            if (existingDll.LocalRecord?.IsDownloaded == true)
            {
                return DLLImportResult.FromSucces(zippedDllFullName ?? filePath, $"{fileName} (already imported)", false);
            }
            importingAsDownloadedDll = true;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var dllRecord = existingDll ?? new DLLRecord()
            {
                Version = versionInfo.GetFormattedFileVersion(),
                VersionNumber = versionInfo.GetFileVersionNumber(),
                MD5Hash = dllHash,
                FileSize = fileInfo.Length,
                ZipFileSize = 0,
                ZipMD5Hash = string.Empty,
                IsSignatureValid = isTrusted,
                AssetType = gameAssetType.Value,
            };


            // TODO: Get extra data from DLL if possible


            var zipFilename = dllRecord.GetExpectedZipName();
            var finalZipOutputPath = GetExpectedZipPath(dllRecord, !importingAsDownloadedDll);
            if (string.IsNullOrWhiteSpace(finalZipOutputPath))
            {
                return DLLImportResult.FromFail(zippedDllFullName ?? filePath, "Could not determine import path.");
            }
            Storage.CreateDirectoryIfNotExists(finalZipOutputPath);

            var finalZipPath = Path.Combine(finalZipOutputPath, zipFilename);
   
            var tempExtractPath = Path.Combine(Storage.GetTemp(), "import");
            Storage.CreateDirectoryIfNotExists(tempExtractPath);

            var tempZipFile = Path.Combine(tempExtractPath, zipFilename);

            using (var zipFile = File.Open(tempZipFile, FileMode.Create))
            {
                using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create, true))
                {
                    zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(fileName));
                }

                zipFile.Position = 0;

                dllRecord.ZipFileSize = zipFile.Length;
                // Once again, MD5 should never be used to check if a file has been tampered with.
                // We are simply using it to check the integrity of the downloaded/extracted file.
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(zipFile);
                    dllRecord.ZipMD5Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }

            // Move new record to where it should live
            File.Move(tempZipFile, finalZipPath, true);
            var newLocalRecord = LocalRecord.FromExpectedPath(finalZipPath, !importingAsDownloadedDll);

            App.CurrentApp.RunOnUIThread(() =>
            {
                dllRecord.LocalRecord = null;
                dllRecord.LocalRecord = newLocalRecord;
            });

            // Add our new record.
            if (importingAsDownloadedDll == true)
            {
                // NOOP - DLL is already in the list, we just updated the LocalRecord for it.
            }
            else
            {
                // Insert into the main DLL list
                var tempList = new List<DLLRecord>(recordList);
                var insertIndex = tempList.BinarySearch(dllRecord);
                if (insertIndex < 0)
                {
                    insertIndex = ~insertIndex;
                }
                App.CurrentApp.RunOnUIThread(() =>
                {
                    recordList.Insert(insertIndex, dllRecord);
                });

                // Insert into the list used for local manifest
                insertIndex = importedRecordList.BinarySearch(dllRecord);
                if (insertIndex < 0)
                {
                    insertIndex = ~insertIndex;
                }
                importedRecordList.Insert(insertIndex, dllRecord);
            }

            return DLLImportResult.FromSucces(zippedDllFullName ?? filePath, fileName, importingAsDownloadedDll);
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, err.Message);
        }
    }

    internal void DeleteImportedDllRecord(DLLRecord dllRecord)
    {
        ObservableCollection<DLLRecord>? recordList = null;
        List<DLLRecord>? importedRecordList = null;

        if (dllRecord.AssetType == GameAssetType.DLSS)
        {
            recordList = DLSSRecords;
            importedRecordList = ImportedManifest?.DLSS;
        }
        else if (dllRecord.AssetType == GameAssetType.DLSS_G)
        {
            recordList = DLSSGRecords;
            importedRecordList = ImportedManifest?.DLSS_G;
        }
        else if (dllRecord.AssetType == GameAssetType.DLSS_D)
        {
            recordList = DLSSDRecords;
            importedRecordList = ImportedManifest?.DLSS_D;
        }
        else if (dllRecord.AssetType == GameAssetType.FSR_31_DX12)
        {
            recordList = FSR31DX12Records;
            importedRecordList = ImportedManifest?.FSR_31_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.FSR_31_VK)
        {
            recordList = FSR31VKRecords;
            importedRecordList = ImportedManifest?.FSR_31_VK;
        }
        else if (dllRecord.AssetType == GameAssetType.XeSS)
        {
            recordList = XeSSRecords;
            importedRecordList = ImportedManifest?.XeSS;
        }
        else if (dllRecord.AssetType == GameAssetType.XeLL)
        {
            recordList = XeLLRecords;
            importedRecordList = ImportedManifest?.XeLL;
        }
        else if (dllRecord.AssetType == GameAssetType.XeSS_FG)
        {
            recordList = XeSSFGRecords;
            importedRecordList = ImportedManifest?.XeSS_FG;
        }

        if (recordList is null)
        {
            // For some reason we couldn't get the recordList, is this a new DLL type?
            Debugger.Break();
            return;
        }

        recordList.Remove(dllRecord);
        importedRecordList?.Remove(dllRecord);
    }
}
