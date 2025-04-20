using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;

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

    internal Manifest? Manifest { get; private set; }
    internal Manifest? ImportedManifest { get; private set; }

    public async Task LoadManifestsAsync()
    {
        // Try load the manifest.
        var manifestFile = Storage.GetManifestPath();
        if (File.Exists(manifestFile))
        {
            try
            {
                using (var stream = File.OpenRead(manifestFile))
                {
                    var manifest = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Manifest).ConfigureAwait(false);
                    if (manifest is not null)
                    {
                        Manifest = manifest;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        // If we could not load the dynamic manifest, try the static one
        if (Manifest is null)
        {
            Logger.Info("No manifest loaded, loading static manifest instead.");
            try
            {
                using (var staticManifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLSS_Swapper.Assets.static_manifest.json"))
                {
                    if (staticManifestStream is not null)
                    {
                        var manifest = await JsonSerializer.DeserializeAsync(staticManifestStream, SourceGenerationContext.Default.Manifest).ConfigureAwait(false);
                        if (manifest is not null)
                        {
                            Logger.Info("Loaded static manifest");
                            Manifest = manifest;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }

        // If we were still unable to load it, it will be loaded in UpdateManifestIfOldAsync.
        // If it isn't loaded there we error out for the user.
        if (Manifest is null)
        {
            Logger.Error("Could not load dynamic or static manifest. Attempting to load remote soon.");
        }

        // Load the imported manifest. If we can't load it we keep it as null. If the file does not exist we don't
        // create a new one as the user may not even be using that feature.
        var importedManifestFile = Storage.GetImportedManifestPath();
        if (File.Exists(importedManifestFile) == true)
        {
            try
            {
                using (var stream = File.OpenRead(importedManifestFile))
                {
                    var importedManifest = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Manifest).ConfigureAwait(false);
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
            // We don't save the new imported manifest until its actually changed.
            ImportedManifest = new Manifest();
        }

        // If we couldn't load the ImportedManifest we will disable the import system.
        // This helps with preventing overriding of user data.
        if (ImportedManifest is null)
        {
            Logger.Error("Could not load imported manifest, disabling import system.");
        }

        await ProcessManifestsAsync();
    }


    /// <summary>
    /// Saves manifest to the dynamic manifest.json file.
    /// </summary>
    internal async Task SaveManifestJsonAsync()
    {
        // Don't attempt to save it if we never loaded it.
        if (Manifest is null)
        {
            return;
        }

        var manifestPath = Storage.GetManifestPath();
        try
        {
            Storage.CreateDirectoryForFileIfNotExists(manifestPath);
            using (var stream = File.Create(manifestPath))
            {
                await JsonSerializer.SerializeAsync(stream, Manifest, SourceGenerationContext.Default.Manifest).ConfigureAwait(false);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            Debugger.Break();
        }
    }


    /// <summary>
    /// Checks if the manifest is out of date (3 hours old), and if it is we will attempt to reload it.
    /// </summary>
    internal async Task UpdateManifestIfOldAsync()
    {
        var shouldUpdate = false;
        var manifestFile = Storage.GetManifestPath();

        if (Manifest is null)
        {
            shouldUpdate = true;
        }
        else if (File.Exists(manifestFile))
        {
            var fileInfo = new FileInfo(manifestFile);

            // If the manifest is > 3h old
            var timeSinceLastUpdate = DateTimeOffset.Now - fileInfo.LastWriteTime;
            if (timeSinceLastUpdate.TotalHours > 5)
            {
                shouldUpdate = true;
            }
        }
        else
        {
            // Update manifest if it is not found.
            shouldUpdate = true;
        }

        if (shouldUpdate)
        {
            await UpdateManifestAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads a new manifest from the internet and saves it.
    /// </summary>
    /// <returns>Boolean of if we were able to fetch the manifest from the remote source.</returns>
    internal async Task<bool> UpdateManifestAsync()
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                // TODO: Check how quickly this takes to timeout if there is no internet connection. Consider 
                // adding a "fast UpdateManifest" which will quit early if we were unable to load in 10sec 
                // which would then fall back to loading local.
                var fileDownloader = new FileDownloader("https://raw.githubusercontent.com/beeradmoore/dlss-swapper-manifest-builder/refs/heads/main/manifest.json", 0);
                await fileDownloader.DownloadFileToStreamAsync(memoryStream);

                memoryStream.Position = 0;

                var manifest = await JsonSerializer.DeserializeAsync(memoryStream, SourceGenerationContext.Default.Manifest);
                if (manifest is null)
                {
                    throw new Exception("Could not deserialize manifest.json.");
                }

                Manifest = manifest;

                await SaveManifestJsonAsync().ConfigureAwait(false);
                await ProcessManifestsAsync().ConfigureAwait(false);

                return true;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            Debugger.Break();
            return false;
        }
    }

    /// <summary>
    /// Processes manifest and imported manifest objects to the current DLL records lists.
    /// </summary>
    async Task ProcessManifestsAsync()
    {
        // If manifest is not loaded we can't do anything.
        if (Manifest is null)
        {
            return;
        }

        // Update the KnownDLLs list
        _knownDLLsReadWriterLock.EnterWriteLock();
        try
        {
            KnownDLLs = Manifest.KnownDLLs;
        }
        finally
        {
            _knownDLLsReadWriterLock.ExitWriteLock();
        }

        // Cancel downloading of all current DLL records
        CancelDownloads(DLSSRecords);
        CancelDownloads(DLSSGRecords);
        CancelDownloads(DLSSDRecords);
        CancelDownloads(FSR31DX12Records);
        CancelDownloads(FSR31VKRecords);
        CancelDownloads(XeSSRecords);
        CancelDownloads(XeSSFGRecords);
        CancelDownloads(XeLLRecords);

        // Update incoming DLL record game asset types
        SetGameAssetType(Manifest.DLSS, GameAssetType.DLSS);
        SetGameAssetType(Manifest.DLSS_D, GameAssetType.DLSS_D);
        SetGameAssetType(Manifest.DLSS_G, GameAssetType.DLSS_G);
        SetGameAssetType(Manifest.FSR_31_DX12, GameAssetType.FSR_31_DX12);
        SetGameAssetType(Manifest.FSR_31_VK, GameAssetType.FSR_31_VK);
        SetGameAssetType(Manifest.XeSS, GameAssetType.DLSS_D);
        SetGameAssetType(Manifest.XeSS_FG, GameAssetType.XeSS_FG);
        SetGameAssetType(Manifest.XeLL, GameAssetType.XeLL);
        if (ImportedManifest is not null)
        {
            SetGameAssetType(ImportedManifest.DLSS, GameAssetType.DLSS);
            SetGameAssetType(ImportedManifest.DLSS_D, GameAssetType.DLSS_D);
            SetGameAssetType(ImportedManifest.DLSS_G, GameAssetType.DLSS_G);
            SetGameAssetType(ImportedManifest.FSR_31_DX12, GameAssetType.FSR_31_DX12);
            SetGameAssetType(ImportedManifest.FSR_31_VK, GameAssetType.FSR_31_VK);
            SetGameAssetType(ImportedManifest.XeSS, GameAssetType.DLSS_D);
            SetGameAssetType(ImportedManifest.XeSS_FG, GameAssetType.XeSS_FG);
            SetGameAssetType(ImportedManifest.XeLL, GameAssetType.XeLL);
        }

        // Migrate records from zip to raw dlls
        CheckDllRecordsForMigration_117(Manifest.DLSS, ImportedManifest?.DLSS);
        CheckDllRecordsForMigration_117(Manifest.DLSS_D, ImportedManifest?.DLSS_D);
        CheckDllRecordsForMigration_117(Manifest.DLSS_G, ImportedManifest?.DLSS_G);
        CheckDllRecordsForMigration_117(Manifest.FSR_31_DX12, ImportedManifest?.FSR_31_DX12);
        CheckDllRecordsForMigration_117(Manifest.FSR_31_VK, ImportedManifest?.FSR_31_VK);
        CheckDllRecordsForMigration_117(Manifest.XeSS, ImportedManifest?.XeSS);
        CheckDllRecordsForMigration_117(Manifest.XeSS_FG, ImportedManifest?.XeSS_FG);
        CheckDllRecordsForMigration_117(Manifest.XeLL, ImportedManifest?.XeLL);

        // Load local records
        LoadLocalRecords(Manifest.DLSS);
        LoadLocalRecords(Manifest.DLSS_D);
        LoadLocalRecords(Manifest.DLSS_G);
        LoadLocalRecords(Manifest.FSR_31_DX12);
        LoadLocalRecords(Manifest.FSR_31_VK);
        LoadLocalRecords(Manifest.XeSS);
        LoadLocalRecords(Manifest.XeSS_FG);
        LoadLocalRecords(Manifest.XeLL);
        if (ImportedManifest is not null)
        {
            LoadLocalRecords(ImportedManifest.DLSS, true);
            LoadLocalRecords(ImportedManifest.DLSS_D, true);
            LoadLocalRecords(ImportedManifest.DLSS_G, true);
            LoadLocalRecords(ImportedManifest.FSR_31_DX12, true);
            LoadLocalRecords(ImportedManifest.FSR_31_VK, true);
            LoadLocalRecords(ImportedManifest.XeSS, true);
            LoadLocalRecords(ImportedManifest.XeSS_FG, true);
            LoadLocalRecords(ImportedManifest.XeLL, true);
        }
               
        // See if there is any imported manifest items that are to be migrated to downloaded
        // CheckImportedManifestForCleanUp needs to be called after LoadLocalRecords
        var didChangeImportedManifest = false;
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS, ImportedManifest?.DLSS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS_D, ImportedManifest?.DLSS_D);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS_G, ImportedManifest?.DLSS_G);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FSR_31_DX12, ImportedManifest?.FSR_31_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FSR_31_VK, ImportedManifest?.FSR_31_VK);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeSS, ImportedManifest?.XeSS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeSS_FG, ImportedManifest?.XeSS_FG);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeLL, ImportedManifest?.XeLL);

        if (didChangeImportedManifest == true)
        {
            await SaveImportedManifestJsonAsync().ConfigureAwait(false);
        }

        App.CurrentApp.RunOnUIThread(() =>
        {
            // Merge each of the manifests into the master DLL record list
            MergeManifestsIntoMasterList(GameAssetType.DLSS, DLSSRecords, Manifest.DLSS, ImportedManifest?.DLSS);
            MergeManifestsIntoMasterList(GameAssetType.DLSS_G, DLSSGRecords, Manifest.DLSS_G, ImportedManifest?.DLSS_G);
            MergeManifestsIntoMasterList(GameAssetType.DLSS_D, DLSSDRecords, Manifest.DLSS_D, ImportedManifest?.DLSS_D);
            MergeManifestsIntoMasterList(GameAssetType.FSR_31_DX12, FSR31DX12Records, Manifest.FSR_31_DX12, ImportedManifest?.FSR_31_DX12);
            MergeManifestsIntoMasterList(GameAssetType.FSR_31_VK, FSR31VKRecords, Manifest.FSR_31_VK, ImportedManifest?.FSR_31_VK);
            MergeManifestsIntoMasterList(GameAssetType.XeSS, XeSSRecords, Manifest.XeSS, ImportedManifest?.XeSS);
            MergeManifestsIntoMasterList(GameAssetType.XeSS_FG, XeSSFGRecords, Manifest.XeSS_FG, ImportedManifest?.XeSS_FG);
            MergeManifestsIntoMasterList(GameAssetType.XeLL, XeLLRecords, Manifest.XeLL, ImportedManifest?.XeLL);
        });
    }

    static void CancelDownloads(ObservableCollection<DLLRecord> dllRecords)
    {
        foreach (var dllRecord in dllRecords)
        {
            dllRecord.CancelDownload();
        }
    }

    /// <summary>
    /// Updates every dllRecord to have the specific gameAssetType
    /// </summary>
    /// <param name="dllRecords"></param>
    /// <param name="gameAssetType"></param>
    static void SetGameAssetType(List<DLLRecord> dllRecords, GameAssetType gameAssetType)
    {
        foreach (var dllRecord in dllRecords)
        {
            dllRecord.AssetType = gameAssetType;
        }
    }

    /// <summary>
    /// Looks through each DllRecord and see if they need to be migrated to new folder structure in v1.1.7
    ///
    /// This needs to be called before LoadLocalRecords
    /// </summary>
    /// <param name="dllRecords"></param>
    /// <param name="importedDllRecords"></param>
    /// <returns></returns>
    static void CheckDllRecordsForMigration_117(List<DLLRecord> dllRecords, List<DLLRecord>? importedDllRecords)
    {
        foreach (var dllRecord in dllRecords)
        {
            CheckDllRecordForMigration_117(dllRecord, false);
        }

        if (importedDllRecords is not null)
        {
            foreach (var dllRecord in dllRecords)
            {
                CheckDllRecordForMigration_117(dllRecord, true);
            }
        }
    }

    /// <summary>
    /// As of v1.1.7 we migrated DLLs from being in a zip folder to being a DLL in a folder.
    /// This method will move where the zip was to where the dll will be.
    /// </summary>
    /// <param name="dllRecord"></param>
    /// <param name="isImported"></param>
    static void CheckDllRecordForMigration_117(DLLRecord dllRecord, bool isImported)
    {
        // From GetExpectedZipPath
        var recordType = dllRecord.GetRecordSimpleType();
        if (recordType == string.Empty)
        {
            return;
        }

        var zipPath = Path.Combine(Storage.GetStorageFolder(), (isImported ? $"imported_{recordType}_zip" : $"{recordType}_zip"));
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            return;
        }

        // If the zip path does not exist then we don't need to continue any further.
        if (Directory.Exists(zipPath) == false)
        {
            return;
        }

        var legacyExpectedPath = Path.Combine(zipPath, $"{dllRecord.Version}_{dllRecord.MD5Hash}.zip");
        if (File.Exists(legacyExpectedPath) == false)
        {
            return;
        }

        var dllPath = GetExpectedDllFileName(dllRecord, isImported);
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            return;
        }


        var dllName = Path.GetFileName(dllPath);
        if (string.IsNullOrWhiteSpace(dllName))
        {
            return;
        }

        Storage.CreateDirectoryForFileIfNotExists(dllPath);

        var didExtract = false;

        try
        {
            using (var fileStream = File.OpenRead(legacyExpectedPath))
            {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    var dllEntry = zipArchive.Entries.Single(x => x.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase));
                    dllEntry.ExtractToFile(dllPath, true);
                    didExtract = true;
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, $"Could not extract {legacyExpectedPath} to {dllPath}.");
        }

        if (didExtract == true)
        {
            try
            {
                // Delete the zip we moved
                File.Delete(legacyExpectedPath);
            }
            catch (Exception err)
            {
                Logger.Error(err, $"Could not delete {legacyExpectedPath}");
            }

            // If the old zip father is empty we can delete it.
            if (Directory.GetFiles(zipPath).Length == 0 && Directory.GetDirectories(zipPath).Length == 0)
            {
                try
                {
                    Directory.Delete(zipPath);
                }
                catch (Exception err)
                {
                    Logger.Error(err, $"Could not delete {zipPath}");
                }
            }
        }
    }

    /// <summary>
    /// Looks through each of the imported DLL records to see if they:
    /// - Need to be deleted because the file no longer exists
    /// - Need to be migrated from imported to standard manifest
    ///
    /// This needs to be called after LoadLocalRecords
    /// </summary>
    /// <param name="dllRecords"></param>
    /// <param name="importedDllRecords"></param>
    /// <returns></returns>
    static bool CheckImportedManifestForCleanUp(List<DLLRecord> dllRecords, List<DLLRecord>? importedDllRecords)
    {
        var didChangeImportedManifestList = false;

        if (importedDllRecords is not null)
        {
            var importedDllRecordsToDelete = new List<DLLRecord>();

            // Delete imported DLLs if the file is no longer found.
            foreach (var importedDllRecord in importedDllRecords)
            {
                // If IsDownloaded is false it means the DLL does not exist on the disk
                if (importedDllRecord.LocalRecord?.IsDownloaded == false)
                {
                    Logger.Info($"Imported file not found ({importedDllRecord.LocalRecord}), deleting imported record.");
                    importedDllRecordsToDelete.Add(importedDllRecord);
                }
            }

            // Check if imported DLLs are in the new manifest. If they are we want to
            // move them and pretend they were imported.
            foreach (var importedDllRecord in importedDllRecords)
            {
                // Skip the imported DLL if we are about to remove it.
                if (importedDllRecordsToDelete.Contains(importedDllRecord))
                {
                    continue;
                }

                var manifestDllRecord = dllRecords.FirstOrDefault(x => x.MD5Hash == importedDllRecord.MD5Hash);

                // Make sure both records have a local record.
                if (manifestDllRecord?.LocalRecord is not null && importedDllRecord.LocalRecord is not null)
                {
                    try
                    {
                        // If the DLL is downloaded there is nothing else to change here. Delete the imported one.
                        if (manifestDllRecord.LocalRecord.IsDownloaded == true)
                        {
                            importedDllRecordsToDelete.Add(importedDllRecord);
                            continue;
                        }

                        var oldZipPath = importedDllRecord.LocalRecord.ExpectedPath;
                        if (File.Exists(oldZipPath) == false)
                        {
                            // This should never happen.
                            Logger.Error($"oldZipPath ({oldZipPath}) does not exist.");
                            Debugger.Break();
                            continue;
                        }

                        var expectedPath = Path.GetDirectoryName(manifestDllRecord.LocalRecord.ExpectedPath);
                        if (string.IsNullOrWhiteSpace(expectedPath))
                        {
                            continue;
                        }

                        if (Directory.Exists(expectedPath) == false)
                        {
                            Directory.CreateDirectory(expectedPath);
                        }

                        File.Move(importedDllRecord.LocalRecord.ExpectedPath, manifestDllRecord.LocalRecord.ExpectedPath);

                        App.CurrentApp.RunOnUIThread(() =>
                        {
                            manifestDllRecord.LocalRecord.IsDownloaded = true;
                        });

                        importedDllRecordsToDelete.Add(importedDllRecord);
                        Logger.Info($"Moving imported record to be local record, {importedDllRecord.LocalRecord.ExpectedPath} -> {manifestDllRecord.LocalRecord.ExpectedPath}");
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err);
                        Debugger.Break();
                    }
                }
            }


            // If any of the imported DLLs need to be removed from the imported DLL list.
            if (importedDllRecordsToDelete.Count > 0)
            {
                foreach (var dllRecord in importedDllRecordsToDelete)
                {
                    var dllRecordPath = dllRecord.LocalRecord?.ExpectedPath;
                    if (string.IsNullOrWhiteSpace(dllRecordPath) == true && File.Exists(dllRecordPath))
                    {
                        try
                        {
                            File.Delete(dllRecordPath);
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err, $"Could not delete {dllRecordPath}");
                        }
                    }

                    importedDllRecords.Remove(dllRecord);
                }

                didChangeImportedManifestList = true;
            }
        }

        return didChangeImportedManifestList;
    }

    /// <summary>
    /// Loads the LocalRecrod object on every dllRecord in the list.
    /// </summary>
    /// <param name="dllRecords"></param>
    void LoadLocalRecords(List<DLLRecord> dllRecords, bool isImported = false)
    {
        foreach (var dllRecord in dllRecords)
        {
            LoadLocalRecord(dllRecord, isImported);
        }
    }

    void LoadLocalRecord(DLLRecord dllRecord, bool isImported)
    {
        // If we are loading a new LocalRecord we should cancel existing download.
        dllRecord.CancelDownload();

        // Null out the existing record so we can tell if loading failed.
        App.CurrentApp.RunOnUIThread(() =>
        {
            dllRecord.LocalRecord = null;
        });

        var expectedPath = GetExpectedDllFileName(dllRecord, isImported);
        if (string.IsNullOrWhiteSpace(expectedPath))
        {
            return;
        }

        var localRecord = LocalRecord.FromExpectedPath(expectedPath, isImported);
        App.CurrentApp.RunOnUIThread(() =>
        {
            dllRecord.LocalRecord = localRecord;
        });
    }


    /// <summary>
    /// Takes DLL list from manifest and imported manifest and inserts them into the master DLL records list which is bindable in the app.
    /// </summary>
    /// <param name="gameAssetType"></param>
    /// <param name="records"></param>
    /// <param name="manifestRecords"></param>
    /// <param name="importedRecords"></param>
    /// <returns>Returns true if importedRecords was changed and requires saving</returns>
    static void MergeManifestsIntoMasterList(GameAssetType gameAssetType, ObservableCollection<DLLRecord> records, List<DLLRecord> manifestRecords, List<DLLRecord>? importedManifestRecords)
    {
        // Sort the lists first to ensure local sort, not remote sort.
        manifestRecords.Sort();
        importedManifestRecords?.Sort();

        var tempRecords = new List<DLLRecord>(records);
      
            foreach (var dllRecord in manifestRecords)
            {
                // LoadLocalRecord(dllRecord, false);

                var insertIndex = tempRecords.BinarySearch(dllRecord);
                if (insertIndex < 0) // InsertObject
                {
                    insertIndex = ~insertIndex;


                    records.Insert(insertIndex, dllRecord);

                    tempRecords.Insert(insertIndex, dllRecord);
                }
                else // Update object
                {
                    records[insertIndex].CopyFrom(dllRecord);
                    tempRecords[insertIndex] = dllRecord;
                }
            }

            // Now that we have loaded DLL records we want to add the importedRecords back into that list. 
            if (importedManifestRecords?.Any() == true)
            {
                foreach (var importedRecord in importedManifestRecords)
                {
                    var insertIndex = tempRecords.BinarySearch(importedRecord);
                    if (insertIndex < 0)
                    {
                        insertIndex = ~insertIndex;
                        records.Insert(insertIndex, importedRecord);
                        tempRecords.Insert(insertIndex, importedRecord);
                    }
                    else
                    {
                        records[insertIndex].CopyFrom(importedRecord);
                        tempRecords[insertIndex] = importedRecord;
                    }
                }
            }

    }

    internal bool HasLoadedManifest()
    {
        return Manifest is not null;
    }

    internal bool HasLoadedImportedManifest()
    {
        return ImportedManifest is not null;
    }

    internal async Task<bool> SaveImportedManifestJsonAsync()
    {
        if (ImportedManifest is null)
        {
            Logger.Error("Could not save imported manifest as importing system is disabled.");
            return false;
        }

        var importedManifestFile = Storage.GetImportedManifestPath();
        try
        {
            using (var stream = File.Open(importedManifestFile, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(stream, ImportedManifest, SourceGenerationContext.Default.Manifest);
            }
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return false;
        }
    }

    static string GetExpectedDllFileName(DLLRecord dllRecord, bool isImported)
    {
        var dllPath = GetExpectedDllPath(dllRecord, isImported);
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            return string.Empty;
        }

        var dllName = DllNameForGameAssetType(dllRecord.AssetType);
        if (string.IsNullOrWhiteSpace(dllName))
        {
            return string.Empty;
        }

        return Path.Combine(dllPath, dllName);

    }
    static string GetExpectedDllPath(DLLRecord dllRecord, bool isImported)
    {
        var recordType = dllRecord.GetRecordSimpleType();

        var dllsPath = Path.Combine(Storage.GetStorageFolder(), "dlls", (isImported ? $"imported" : string.Empty), recordType);
        if (string.IsNullOrWhiteSpace(dllsPath))
        {
            return string.Empty;
        }

        var individualDllPath = Path.Combine(dllsPath, $"{recordType}_v{dllRecord.Version}_{dllRecord.MD5Hash}");
        if (string.IsNullOrWhiteSpace(individualDllPath))
        {
            return string.Empty;
        }

        return individualDllPath;
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

            var expectedPath = GetExpectedDllFileName(dllRecord, true);
            if (string.IsNullOrWhiteSpace(expectedPath))
            {
                return DLLImportResult.FromFail(zippedDllFullName ?? filePath, "Could not import DLL.");
            }
            Storage.CreateDirectoryForFileIfNotExists(expectedPath);

            // Move new record to where it should live
            File.Copy(filePath, expectedPath, true);
            var newLocalRecord = LocalRecord.FromExpectedPath(expectedPath, !importingAsDownloadedDll);

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
                var importedInsertIndex = importedRecordList.BinarySearch(dllRecord);
                if (importedInsertIndex < 0)
                {
                    importedInsertIndex = ~importedInsertIndex;
                }
                importedRecordList.Insert(importedInsertIndex, dllRecord);
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

    internal static string DllNameForGameAssetType(GameAssetType gameAssetType)
    {
        return gameAssetType switch
        {
            GameAssetType.DLSS => "nvngx_dlss.dll",
            GameAssetType.DLSS_G => "nvngx_dlssg.dll",
            GameAssetType.DLSS_D => "nvngx_dlssd.dll",
            GameAssetType.FSR_31_DX12 => "amd_fidelityfx_dx12.dll",
            GameAssetType.FSR_31_VK => "amd_fidelityfx_vk.dll",
            GameAssetType.XeSS => "libxess.dll",
            GameAssetType.XeSS_FG => "libxess_fg.dll",
            GameAssetType.XeLL => "libxell.dll",
            _ => string.Empty,
        };
    }
}
