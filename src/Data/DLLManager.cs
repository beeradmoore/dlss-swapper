using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data;

internal class DLLManager
{
    public static DLLManager Instance { get; private set; } = new DLLManager();

    // NOTE: DLL type
    public ObservableCollection<DLLRecord> DLSSRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DLSSGRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DLSSDRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DLSSNRRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FSR31DX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FSR31VKRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeSSRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeLLRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeSSFGRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> XeSSDX11Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DirectStorageRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DirectStorageCoreRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FidelityFXSDK2DenoiserDX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FidelityFXSDK2FrameGenerationDX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FidelityFXSDK2LoaderDX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FidelityFXSDK2RadianceCacheDX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> FidelityFXSDK2UpscalerDX12Records { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineReflexRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlinePCLRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineNvPerfRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineNISRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineInterposerRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDLSSGRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDLSSDRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDLSSNRRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDLSSRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDirectSRRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineDeepDVCRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> StreamlineCommonRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> DeepDVCRecords { get; } = new ObservableCollection<DLLRecord>();
    public ObservableCollection<DLLRecord> NvLowLatencyVKRecords { get; } = new ObservableCollection<DLLRecord>();

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
    /// Loads a new manifest from the internet and saves it.
    /// </summary>
    /// <returns>Boolean of if we were able to fetch the manifest from the remote source.</returns>
    internal async Task<bool> UpdateManifestAsync()
    {
        try
        {
            var oldManifestHash = string.Empty;

            var manifestPath = Storage.GetManifestPath();
            if (File.Exists(manifestPath))
            {
                using (var fileStream = File.OpenRead(manifestPath))
                {
                    oldManifestHash = fileStream.GetMD5Hash();
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                // TODO: Check how quickly this takes to timeout if there is no internet connection. Consider
                // adding a "fast UpdateManifest" which will quit early if we were unable to load in 10sec
                // which would then fall back to loading local.
                var fileDownloader = new FileDownloader("https://beeradmoore.github.io/dlss-swapper/manifest.json", 0);
                await fileDownloader.DownloadFileToStreamAsync(memoryStream);

                memoryStream.Position = 0;

                var newManifestHash = memoryStream.GetMD5Hash();

                // If the old manifest on disk is the same as the new one there is no need to do anything as it will already be loaded.
                if (oldManifestHash == newManifestHash)
                {
                    return true;
                }

                memoryStream.Position = 0;

                var manifest = await JsonSerializer.DeserializeAsync(memoryStream, SourceGenerationContext.Default.Manifest);
                if (manifest is null)
                {
                    throw new Exception("Could not deserialize manifest.json.");
                }

                Manifest = manifest;

                try
                {
                    Storage.CreateDirectoryForFileIfNotExists(manifestPath);
                    using (var stream = File.Create(manifestPath))
                    {
                        memoryStream.Position = 0;
                        memoryStream.CopyTo(stream);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err);
                    Debugger.Break();
                }

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

        // NOTE: DLL type
        // Cancel downloading of all current DLL records
        CancelDownloads(DLSSRecords);
        CancelDownloads(DLSSGRecords);
        CancelDownloads(DLSSDRecords);
        CancelDownloads(DLSSNRRecords);
        CancelDownloads(FSR31DX12Records);
        CancelDownloads(FSR31VKRecords);
        CancelDownloads(XeSSRecords);
        CancelDownloads(XeSSFGRecords);
        CancelDownloads(XeSSDX11Records);
        CancelDownloads(XeLLRecords);
        CancelDownloads(DirectStorageRecords);
        CancelDownloads(DirectStorageCoreRecords);
        CancelDownloads(FidelityFXSDK2DenoiserDX12Records);
        CancelDownloads(FidelityFXSDK2FrameGenerationDX12Records);
        CancelDownloads(FidelityFXSDK2LoaderDX12Records);
        CancelDownloads(FidelityFXSDK2RadianceCacheDX12Records);
        CancelDownloads(FidelityFXSDK2UpscalerDX12Records);
        CancelDownloads(StreamlineReflexRecords);
        CancelDownloads(StreamlinePCLRecords);
        CancelDownloads(StreamlineNvPerfRecords);
        CancelDownloads(StreamlineNISRecords);
        CancelDownloads(StreamlineInterposerRecords);
        CancelDownloads(StreamlineDLSSGRecords);
        CancelDownloads(StreamlineDLSSDRecords);
        CancelDownloads(StreamlineDLSSNRRecords);
        CancelDownloads(StreamlineDLSSRecords);
        CancelDownloads(StreamlineDirectSRRecords);
        CancelDownloads(StreamlineDeepDVCRecords);
        CancelDownloads(StreamlineCommonRecords);
        CancelDownloads(DeepDVCRecords);
        CancelDownloads(NvLowLatencyVKRecords);

        // NOTE: DLL type
        // Update incoming DLL record game asset types
        SetGameAssetType(Manifest.DLSS, GameAssetType.DLSS);
        SetGameAssetType(Manifest.DLSS_D, GameAssetType.DLSS_D);
        SetGameAssetType(Manifest.DLSS_G, GameAssetType.DLSS_G);
        SetGameAssetType(Manifest.DLSS_NR, GameAssetType.DLSS_NR);
        SetGameAssetType(Manifest.FSR_31_DX12, GameAssetType.FSR_31_DX12);
        SetGameAssetType(Manifest.FSR_31_VK, GameAssetType.FSR_31_VK);
        SetGameAssetType(Manifest.XeSS, GameAssetType.XeSS);
        SetGameAssetType(Manifest.XeSS_FG, GameAssetType.XeSS_FG);
        SetGameAssetType(Manifest.XeLL, GameAssetType.XeLL);
        SetGameAssetType(Manifest.XeSS_DX11, GameAssetType.XeSS_DX11);
        SetGameAssetType(Manifest.DirectStorage, GameAssetType.DirectStorage);
        SetGameAssetType(Manifest.DirectStorageCore, GameAssetType.DirectStorageCore);
        SetGameAssetType(Manifest.FidelityFX_SDK2_Denoiser_DX12, GameAssetType.FidelityFX_SDK2_Denoiser_DX12);
        SetGameAssetType(Manifest.FidelityFX_SDK2_FrameGeneration_DX12, GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12);
        SetGameAssetType(Manifest.FidelityFX_SDK2_Loader_DX12, GameAssetType.FidelityFX_SDK2_Loader_DX12);
        SetGameAssetType(Manifest.FidelityFX_SDK2_RadianceCache_DX12, GameAssetType.FidelityFX_SDK2_RadianceCache_DX12);
        SetGameAssetType(Manifest.FidelityFX_SDK2_Upscaler_DX12, GameAssetType.FidelityFX_SDK2_Upscaler_DX12);
        SetGameAssetType(Manifest.Streamline_Reflex, GameAssetType.Streamline_Reflex);
        SetGameAssetType(Manifest.Streamline_PCL, GameAssetType.Streamline_PCL);
        SetGameAssetType(Manifest.Streamline_NvPerf, GameAssetType.Streamline_NvPerf);
        SetGameAssetType(Manifest.Streamline_NIS, GameAssetType.Streamline_NIS);
        SetGameAssetType(Manifest.Streamline_Interposer, GameAssetType.Streamline_Interposer);
        SetGameAssetType(Manifest.Streamline_DLSS_G, GameAssetType.Streamline_DLSS_G);
        SetGameAssetType(Manifest.Streamline_DLSS_D, GameAssetType.Streamline_DLSS_D);
        SetGameAssetType(Manifest.Streamline_DLSS_NR, GameAssetType.Streamline_DLSS_NR);
        SetGameAssetType(Manifest.Streamline_DLSS, GameAssetType.Streamline_DLSS);
        SetGameAssetType(Manifest.Streamline_DirectSR, GameAssetType.Streamline_DirectSR);
        SetGameAssetType(Manifest.Streamline_DeepDVC, GameAssetType.Streamline_DeepDVC);
        SetGameAssetType(Manifest.Streamline_Common, GameAssetType.Streamline_Common);
        SetGameAssetType(Manifest.DeepDVC, GameAssetType.DeepDVC);
        SetGameAssetType(Manifest.NvLowLatencyVK, GameAssetType.NvLowLatencyVK);
        if (ImportedManifest is not null)
        {
            // NOTE: DLL type
            SetGameAssetType(ImportedManifest.DLSS, GameAssetType.DLSS);
            SetGameAssetType(ImportedManifest.DLSS_D, GameAssetType.DLSS_D);
            SetGameAssetType(ImportedManifest.DLSS_G, GameAssetType.DLSS_G);
            SetGameAssetType(ImportedManifest.DLSS_NR, GameAssetType.DLSS_NR);
            SetGameAssetType(ImportedManifest.FSR_31_DX12, GameAssetType.FSR_31_DX12);
            SetGameAssetType(ImportedManifest.FSR_31_VK, GameAssetType.FSR_31_VK);
            SetGameAssetType(ImportedManifest.XeSS, GameAssetType.XeSS);
            SetGameAssetType(ImportedManifest.XeSS_FG, GameAssetType.XeSS_FG);
            SetGameAssetType(ImportedManifest.XeLL, GameAssetType.XeLL);
            SetGameAssetType(ImportedManifest.XeSS_DX11, GameAssetType.XeSS_DX11);
            SetGameAssetType(ImportedManifest.DirectStorage, GameAssetType.DirectStorage);
            SetGameAssetType(ImportedManifest.DirectStorageCore, GameAssetType.DirectStorageCore);
            SetGameAssetType(ImportedManifest.FidelityFX_SDK2_Denoiser_DX12, GameAssetType.FidelityFX_SDK2_Denoiser_DX12);
            SetGameAssetType(ImportedManifest.FidelityFX_SDK2_FrameGeneration_DX12, GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12);
            SetGameAssetType(ImportedManifest.FidelityFX_SDK2_Loader_DX12, GameAssetType.FidelityFX_SDK2_Loader_DX12);
            SetGameAssetType(ImportedManifest.FidelityFX_SDK2_RadianceCache_DX12, GameAssetType.FidelityFX_SDK2_RadianceCache_DX12);
            SetGameAssetType(ImportedManifest.FidelityFX_SDK2_Upscaler_DX12, GameAssetType.FidelityFX_SDK2_Upscaler_DX12);
            SetGameAssetType(ImportedManifest.Streamline_Reflex, GameAssetType.Streamline_Reflex);
            SetGameAssetType(ImportedManifest.Streamline_PCL, GameAssetType.Streamline_PCL);
            SetGameAssetType(ImportedManifest.Streamline_NvPerf, GameAssetType.Streamline_NvPerf);
            SetGameAssetType(ImportedManifest.Streamline_NIS, GameAssetType.Streamline_NIS);
            SetGameAssetType(ImportedManifest.Streamline_Interposer, GameAssetType.Streamline_Interposer);
            SetGameAssetType(ImportedManifest.Streamline_DLSS_G, GameAssetType.Streamline_DLSS_G);
            SetGameAssetType(ImportedManifest.Streamline_DLSS_D, GameAssetType.Streamline_DLSS_D);
            SetGameAssetType(ImportedManifest.Streamline_DLSS_NR, GameAssetType.Streamline_DLSS_NR);
            SetGameAssetType(ImportedManifest.Streamline_DLSS, GameAssetType.Streamline_DLSS);
            SetGameAssetType(ImportedManifest.Streamline_DirectSR, GameAssetType.Streamline_DirectSR);
            SetGameAssetType(ImportedManifest.Streamline_DeepDVC, GameAssetType.Streamline_DeepDVC);
            SetGameAssetType(ImportedManifest.Streamline_Common, GameAssetType.Streamline_Common);
            SetGameAssetType(ImportedManifest.DeepDVC, GameAssetType.DeepDVC);
            SetGameAssetType(ImportedManifest.NvLowLatencyVK, GameAssetType.NvLowLatencyVK);
        }


        // Load local records
        LoadLocalRecords(Manifest.DLSS);
        LoadLocalRecords(Manifest.DLSS_D);
        LoadLocalRecords(Manifest.DLSS_G);
        LoadLocalRecords(Manifest.DLSS_NR);
        LoadLocalRecords(Manifest.FSR_31_DX12);
        LoadLocalRecords(Manifest.FSR_31_VK);
        LoadLocalRecords(Manifest.XeSS);
        LoadLocalRecords(Manifest.XeSS_FG);
        LoadLocalRecords(Manifest.XeLL);
        LoadLocalRecords(Manifest.XeSS_DX11);
        LoadLocalRecords(Manifest.DirectStorage);
        LoadLocalRecords(Manifest.DirectStorageCore);
        LoadLocalRecords(Manifest.FidelityFX_SDK2_Denoiser_DX12);
        LoadLocalRecords(Manifest.FidelityFX_SDK2_FrameGeneration_DX12);
        LoadLocalRecords(Manifest.FidelityFX_SDK2_Loader_DX12);
        LoadLocalRecords(Manifest.FidelityFX_SDK2_RadianceCache_DX12);
        LoadLocalRecords(Manifest.FidelityFX_SDK2_Upscaler_DX12);
        LoadLocalRecords(Manifest.Streamline_Reflex);
        LoadLocalRecords(Manifest.Streamline_PCL);
        LoadLocalRecords(Manifest.Streamline_NvPerf);
        LoadLocalRecords(Manifest.Streamline_NIS);
        LoadLocalRecords(Manifest.Streamline_Interposer);
        LoadLocalRecords(Manifest.Streamline_DLSS_G);
        LoadLocalRecords(Manifest.Streamline_DLSS_D);
        LoadLocalRecords(Manifest.Streamline_DLSS_NR);
        LoadLocalRecords(Manifest.Streamline_DLSS);
        LoadLocalRecords(Manifest.Streamline_DirectSR);
        LoadLocalRecords(Manifest.Streamline_DeepDVC);
        LoadLocalRecords(Manifest.Streamline_Common);
        LoadLocalRecords(Manifest.DeepDVC);
        LoadLocalRecords(Manifest.NvLowLatencyVK);
        if (ImportedManifest is not null)
        {
            LoadLocalRecords(ImportedManifest.DLSS, true);
            LoadLocalRecords(ImportedManifest.DLSS_D, true);
            LoadLocalRecords(ImportedManifest.DLSS_G, true);
            LoadLocalRecords(ImportedManifest.DLSS_NR, true);
            LoadLocalRecords(ImportedManifest.FSR_31_DX12, true);
            LoadLocalRecords(ImportedManifest.FSR_31_VK, true);
            LoadLocalRecords(ImportedManifest.XeSS, true);
            LoadLocalRecords(ImportedManifest.XeSS_FG, true);
            LoadLocalRecords(ImportedManifest.XeLL, true);
            LoadLocalRecords(ImportedManifest.XeSS_DX11, true);
            LoadLocalRecords(ImportedManifest.DirectStorage, true);
            LoadLocalRecords(ImportedManifest.DirectStorageCore, true);
            LoadLocalRecords(ImportedManifest.FidelityFX_SDK2_Denoiser_DX12, true);
            LoadLocalRecords(ImportedManifest.FidelityFX_SDK2_FrameGeneration_DX12, true);
            LoadLocalRecords(ImportedManifest.FidelityFX_SDK2_Loader_DX12, true);
            LoadLocalRecords(ImportedManifest.FidelityFX_SDK2_RadianceCache_DX12, true);
            LoadLocalRecords(ImportedManifest.FidelityFX_SDK2_Upscaler_DX12, true);
            LoadLocalRecords(ImportedManifest.Streamline_Reflex, true);
            LoadLocalRecords(ImportedManifest.Streamline_PCL, true);
            LoadLocalRecords(ImportedManifest.Streamline_NvPerf, true);
            LoadLocalRecords(ImportedManifest.Streamline_NIS, true);
            LoadLocalRecords(ImportedManifest.Streamline_Interposer, true);
            LoadLocalRecords(ImportedManifest.Streamline_DLSS_G, true);
            LoadLocalRecords(ImportedManifest.Streamline_DLSS_D, true);
            LoadLocalRecords(ImportedManifest.Streamline_DLSS_NR, true);
            LoadLocalRecords(ImportedManifest.Streamline_DLSS, true);
            LoadLocalRecords(ImportedManifest.Streamline_DirectSR, true);
            LoadLocalRecords(ImportedManifest.Streamline_DeepDVC, true);
            LoadLocalRecords(ImportedManifest.Streamline_Common, true);
            LoadLocalRecords(ImportedManifest.DeepDVC, true);
            LoadLocalRecords(ImportedManifest.NvLowLatencyVK, true);
        }

        // See if there is any imported manifest items that are to be migrated to downloaded
        // CheckImportedManifestForCleanUp needs to be called after LoadLocalRecords
        var didChangeImportedManifest = false;
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS, ImportedManifest?.DLSS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS_D, ImportedManifest?.DLSS_D);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS_G, ImportedManifest?.DLSS_G);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DLSS_NR, ImportedManifest?.DLSS_NR);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FSR_31_DX12, ImportedManifest?.FSR_31_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FSR_31_VK, ImportedManifest?.FSR_31_VK);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeSS, ImportedManifest?.XeSS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeSS_FG, ImportedManifest?.XeSS_FG);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeLL, ImportedManifest?.XeLL);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.XeSS_DX11, ImportedManifest?.XeSS_DX11);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DirectStorage, ImportedManifest?.DirectStorage);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DirectStorageCore, ImportedManifest?.DirectStorageCore);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FidelityFX_SDK2_Denoiser_DX12, ImportedManifest?.FidelityFX_SDK2_Denoiser_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FidelityFX_SDK2_FrameGeneration_DX12, ImportedManifest?.FidelityFX_SDK2_FrameGeneration_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FidelityFX_SDK2_Loader_DX12, ImportedManifest?.FidelityFX_SDK2_Loader_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FidelityFX_SDK2_RadianceCache_DX12, ImportedManifest?.FidelityFX_SDK2_RadianceCache_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.FidelityFX_SDK2_Upscaler_DX12, ImportedManifest?.FidelityFX_SDK2_Upscaler_DX12);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_Reflex, ImportedManifest?.Streamline_Reflex);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_PCL, ImportedManifest?.Streamline_PCL);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_NvPerf, ImportedManifest?.Streamline_NvPerf);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_NIS, ImportedManifest?.Streamline_NIS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_Interposer, ImportedManifest?.Streamline_Interposer);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DLSS_G, ImportedManifest?.Streamline_DLSS_G);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DLSS_D, ImportedManifest?.Streamline_DLSS_D);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DLSS_NR, ImportedManifest?.Streamline_DLSS_NR);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DLSS, ImportedManifest?.Streamline_DLSS);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DirectSR, ImportedManifest?.Streamline_DirectSR);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_DeepDVC, ImportedManifest?.Streamline_DeepDVC);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.Streamline_Common, ImportedManifest?.Streamline_Common);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.DeepDVC, ImportedManifest?.DeepDVC);
        didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.NvLowLatencyVK, ImportedManifest?.NvLowLatencyVK);

        if (didChangeImportedManifest == true)
        {
            await SaveImportedManifestJsonAsync().ConfigureAwait(false);
        }

        App.CurrentApp.RunOnUIThread(() =>
        {
            // NOTE: DLL type
            // Merge each of the manifests into the master DLL record list
            MergeManifestsIntoMasterList(DLSSRecords, Manifest.DLSS, ImportedManifest?.DLSS);
            MergeManifestsIntoMasterList(DLSSGRecords, Manifest.DLSS_G, ImportedManifest?.DLSS_G);
            MergeManifestsIntoMasterList(DLSSDRecords, Manifest.DLSS_D, ImportedManifest?.DLSS_D);
            MergeManifestsIntoMasterList(DLSSNRRecords, Manifest.DLSS_NR, ImportedManifest?.DLSS_NR);
            MergeManifestsIntoMasterList(FSR31DX12Records, Manifest.FSR_31_DX12, ImportedManifest?.FSR_31_DX12);
            MergeManifestsIntoMasterList(FSR31VKRecords, Manifest.FSR_31_VK, ImportedManifest?.FSR_31_VK);
            MergeManifestsIntoMasterList(XeSSRecords, Manifest.XeSS, ImportedManifest?.XeSS);
            MergeManifestsIntoMasterList(XeSSFGRecords, Manifest.XeSS_FG, ImportedManifest?.XeSS_FG);
            MergeManifestsIntoMasterList(XeSSDX11Records, Manifest.XeSS_DX11, ImportedManifest?.XeSS_DX11);
            MergeManifestsIntoMasterList(XeLLRecords, Manifest.XeLL, ImportedManifest?.XeLL);
            MergeManifestsIntoMasterList(DirectStorageRecords, Manifest.DirectStorage, ImportedManifest?.DirectStorage);
            MergeManifestsIntoMasterList(DirectStorageCoreRecords, Manifest.DirectStorageCore, ImportedManifest?.DirectStorageCore);
            MergeManifestsIntoMasterList(FidelityFXSDK2DenoiserDX12Records, Manifest.FidelityFX_SDK2_Denoiser_DX12, ImportedManifest?.FidelityFX_SDK2_Denoiser_DX12);
            MergeManifestsIntoMasterList(FidelityFXSDK2FrameGenerationDX12Records, Manifest.FidelityFX_SDK2_FrameGeneration_DX12, ImportedManifest?.FidelityFX_SDK2_FrameGeneration_DX12);
            MergeManifestsIntoMasterList(FidelityFXSDK2LoaderDX12Records, Manifest.FidelityFX_SDK2_Loader_DX12, ImportedManifest?.FidelityFX_SDK2_Loader_DX12);
            MergeManifestsIntoMasterList(FidelityFXSDK2RadianceCacheDX12Records, Manifest.FidelityFX_SDK2_RadianceCache_DX12, ImportedManifest?.FidelityFX_SDK2_RadianceCache_DX12);
            MergeManifestsIntoMasterList(FidelityFXSDK2UpscalerDX12Records, Manifest.FidelityFX_SDK2_Upscaler_DX12, ImportedManifest?.FidelityFX_SDK2_Upscaler_DX12);
            MergeManifestsIntoMasterList(StreamlineReflexRecords, Manifest.Streamline_Reflex, ImportedManifest?.Streamline_Reflex);
            MergeManifestsIntoMasterList(StreamlinePCLRecords, Manifest.Streamline_PCL, ImportedManifest?.Streamline_PCL);
            MergeManifestsIntoMasterList(StreamlineNvPerfRecords, Manifest.Streamline_NvPerf, ImportedManifest?.Streamline_NvPerf);
            MergeManifestsIntoMasterList(StreamlineNISRecords, Manifest.Streamline_NIS, ImportedManifest?.Streamline_NIS);
            MergeManifestsIntoMasterList(StreamlineInterposerRecords, Manifest.Streamline_Interposer, ImportedManifest?.Streamline_Interposer);
            MergeManifestsIntoMasterList(StreamlineDLSSGRecords, Manifest.Streamline_DLSS_G, ImportedManifest?.Streamline_DLSS_G);
            MergeManifestsIntoMasterList(StreamlineDLSSDRecords, Manifest.Streamline_DLSS_D, ImportedManifest?.Streamline_DLSS_D);
            MergeManifestsIntoMasterList(StreamlineDLSSNRRecords, Manifest.Streamline_DLSS_NR, ImportedManifest?.Streamline_DLSS_NR);
            MergeManifestsIntoMasterList(StreamlineDLSSRecords, Manifest.Streamline_DLSS, ImportedManifest?.Streamline_DLSS);
            MergeManifestsIntoMasterList(StreamlineDirectSRRecords, Manifest.Streamline_DirectSR, ImportedManifest?.Streamline_DirectSR);
            MergeManifestsIntoMasterList(StreamlineDeepDVCRecords, Manifest.Streamline_DeepDVC, ImportedManifest?.Streamline_DeepDVC);
            MergeManifestsIntoMasterList(StreamlineCommonRecords, Manifest.Streamline_Common, ImportedManifest?.Streamline_Common);
            MergeManifestsIntoMasterList(DeepDVCRecords, Manifest.DeepDVC, ImportedManifest?.DeepDVC);
            MergeManifestsIntoMasterList(NvLowLatencyVKRecords, Manifest.NvLowLatencyVK, ImportedManifest?.NvLowLatencyVK);

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
    /// <param name="records"></param>
    /// <param name="manifestRecords"></param>
    /// <param name="importedRecords"></param>
    /// <returns>Returns true if importedRecords was changed and requires saving</returns>
    static void MergeManifestsIntoMasterList(ObservableCollection<DLLRecord> records, List<DLLRecord> manifestRecords, List<DLLRecord>? importedManifestRecords)
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
        // NOTE: DLL type
        return assetType switch
        {
            GameAssetType.DLSS => ResourceHelper.GetString("General_Name_DLSS"),
            GameAssetType.DLSS_G => ResourceHelper.GetString("General_Name_DLSS_G"),
            GameAssetType.DLSS_D => ResourceHelper.GetString("General_Name_DLSS_D"),
            GameAssetType.DLSS_NR => ResourceHelper.GetString("General_Name_DLSS_NR"),
            GameAssetType.FSR_31_DX12 => ResourceHelper.GetString("General_Name_FSR_31_DX12"),
            GameAssetType.FSR_31_VK => ResourceHelper.GetString("General_Name_FSR_31_VK"),
            GameAssetType.XeSS => ResourceHelper.GetString("General_Name_XeSS"),
            GameAssetType.XeSS_FG => ResourceHelper.GetString("General_Name_XeSS_FG"),
            GameAssetType.XeSS_DX11 => ResourceHelper.GetString("General_Name_XeSS_DX11"),
            GameAssetType.XeLL => ResourceHelper.GetString("General_Name_XeLL"),
            GameAssetType.DirectStorage => ResourceHelper.GetString("General_Name_DirectStorage"),
            GameAssetType.DirectStorageCore => ResourceHelper.GetString("General_Name_DirectStorageCore"),
            GameAssetType.FidelityFX_SDK2_Denoiser_DX12 => ResourceHelper.GetString("General_Name_FidelityFX_SDK2_Denoiser_DX12"),
            GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12 => ResourceHelper.GetString("General_Name_FidelityFX_SDK2_FrameGeneration_DX12"),
            GameAssetType.FidelityFX_SDK2_Loader_DX12 => ResourceHelper.GetString("General_Name_FidelityFX_SDK2_Loader_DX12"),
            GameAssetType.FidelityFX_SDK2_RadianceCache_DX12 => ResourceHelper.GetString("General_Name_FidelityFX_SDK2_RadianceCache_DX12"),
            GameAssetType.FidelityFX_SDK2_Upscaler_DX12 => ResourceHelper.GetString("General_Name_FidelityFX_SDK2_Upscaler_DX12"),
            GameAssetType.Streamline_Reflex => ResourceHelper.GetString("General_Name_Streamline_Reflex"),
            GameAssetType.Streamline_PCL => ResourceHelper.GetString("General_Name_Streamline_PCL"),
            GameAssetType.Streamline_NvPerf => ResourceHelper.GetString("General_Name_Streamline_NvPerf"),
            GameAssetType.Streamline_NIS => ResourceHelper.GetString("General_Name_Streamline_NIS"),
            GameAssetType.Streamline_Interposer => ResourceHelper.GetString("General_Name_Streamline_Interposer"),
            GameAssetType.Streamline_DLSS_G => ResourceHelper.GetString("General_Name_Streamline_DLSS_G"),
            GameAssetType.Streamline_DLSS_D => ResourceHelper.GetString("General_Name_Streamline_DLSS_D"),
            GameAssetType.Streamline_DLSS_NR => ResourceHelper.GetString("General_Name_Streamline_DLSS_NR"),
            GameAssetType.Streamline_DLSS => ResourceHelper.GetString("General_Name_Streamline_DLSS"),
            GameAssetType.Streamline_DirectSR => ResourceHelper.GetString("General_Name_Streamline_DirectSR"),
            GameAssetType.Streamline_DeepDVC => ResourceHelper.GetString("General_Name_Streamline_DeepDVC"),
            GameAssetType.Streamline_Common => ResourceHelper.GetString("General_Name_Streamline_Common"),
            GameAssetType.DeepDVC => ResourceHelper.GetString("General_Name_DeepDVC"),
            GameAssetType.NvLowLatencyVK => ResourceHelper.GetString("General_Name_NvLowLatencyVK"),

            _ => throw new Exception($"Unknown AssetType: {assetType}"),
        };
    }


    public GameAssetType GetAssetBackupType(GameAssetType assetType)
    {
        // NOTE: DLL type
        return assetType switch
        {
            GameAssetType.DLSS => GameAssetType.DLSS_BACKUP,
            GameAssetType.DLSS_G => GameAssetType.DLSS_G_BACKUP,
            GameAssetType.DLSS_D => GameAssetType.DLSS_D_BACKUP,
            GameAssetType.DLSS_NR => GameAssetType.DLSS_NR_BACKUP,
            GameAssetType.FSR_31_DX12 => GameAssetType.FSR_31_DX12_BACKUP,
            GameAssetType.FSR_31_VK => GameAssetType.FSR_31_VK_BACKUP,
            GameAssetType.XeSS => GameAssetType.XeSS_BACKUP,
            GameAssetType.XeSS_FG => GameAssetType.XeSS_FG_BACKUP,
            GameAssetType.XeSS_DX11 => GameAssetType.XeSS_DX11_BACKUP,
            GameAssetType.XeLL => GameAssetType.XeLL_BACKUP,
            GameAssetType.DirectStorage => GameAssetType.DirectStorage_BACKUP,
            GameAssetType.DirectStorageCore => GameAssetType.DirectStorageCore_BACKUP,
            GameAssetType.FidelityFX_SDK2_Denoiser_DX12 => GameAssetType.FidelityFX_SDK2_Denoiser_DX12_BACKUP,
            GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12 => GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12_BACKUP,
            GameAssetType.FidelityFX_SDK2_Loader_DX12 => GameAssetType.FidelityFX_SDK2_Loader_DX12_BACKUP,
            GameAssetType.FidelityFX_SDK2_RadianceCache_DX12 => GameAssetType.FidelityFX_SDK2_RadianceCache_DX12_BACKUP,
            GameAssetType.FidelityFX_SDK2_Upscaler_DX12 => GameAssetType.FidelityFX_SDK2_Upscaler_DX12_BACKUP,
            GameAssetType.Streamline_Reflex => GameAssetType.Streamline_Reflex_BACKUP,
            GameAssetType.Streamline_PCL => GameAssetType.Streamline_PCL_BACKUP,
            GameAssetType.Streamline_NvPerf => GameAssetType.Streamline_NvPerf_BACKUP,
            GameAssetType.Streamline_NIS => GameAssetType.Streamline_NIS_BACKUP,
            GameAssetType.Streamline_Interposer => GameAssetType.Streamline_Interposer_BACKUP,
            GameAssetType.Streamline_DLSS_G => GameAssetType.Streamline_DLSS_G_BACKUP,
            GameAssetType.Streamline_DLSS_D => GameAssetType.Streamline_DLSS_D_BACKUP,
            GameAssetType.Streamline_DLSS_NR => GameAssetType.Streamline_DLSS_NR_BACKUP,
            GameAssetType.Streamline_DLSS => GameAssetType.Streamline_DLSS_BACKUP,
            GameAssetType.Streamline_DirectSR => GameAssetType.Streamline_DirectSR_BACKUP,
            GameAssetType.Streamline_DeepDVC => GameAssetType.Streamline_DeepDVC_BACKUP,
            GameAssetType.Streamline_Common => GameAssetType.Streamline_Common_BACKUP,
            GameAssetType.DeepDVC => GameAssetType.DeepDVC_BACKUP,
            GameAssetType.NvLowLatencyVK => GameAssetType.NvLowLatencyVK_BACKUP,
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
        // NOTE: DLL type
        // For each asset type first check if is in the DLSS Swapper manifest
        var recordList = gameAsset.AssetType switch
        {
            GameAssetType.DLSS or GameAssetType.DLSS_BACKUP => DLSSRecords,
            GameAssetType.DLSS_D or GameAssetType.DLSS_D_BACKUP => DLSSDRecords,
            GameAssetType.DLSS_G or GameAssetType.DLSS_G_BACKUP => DLSSGRecords,
            GameAssetType.DLSS_NR or GameAssetType.DLSS_NR_BACKUP => DLSSNRRecords,
            GameAssetType.FSR_31_DX12 or GameAssetType.FSR_31_DX12_BACKUP => FSR31DX12Records,
            GameAssetType.FSR_31_VK or GameAssetType.FSR_31_VK_BACKUP => FSR31VKRecords,
            GameAssetType.XeSS or GameAssetType.XeSS_BACKUP => XeSSRecords,
            GameAssetType.XeLL or GameAssetType.XeLL_BACKUP => XeLLRecords,
            GameAssetType.XeSS_FG or GameAssetType.XeSS_FG_BACKUP => XeSSFGRecords,
            GameAssetType.XeSS_DX11 or GameAssetType.XeSS_DX11_BACKUP => XeSSDX11Records,
            GameAssetType.DirectStorage or GameAssetType.DirectStorage_BACKUP => DirectStorageRecords,
            GameAssetType.DirectStorageCore or GameAssetType.DirectStorageCore_BACKUP => DirectStorageCoreRecords,
            GameAssetType.FidelityFX_SDK2_Denoiser_DX12 or GameAssetType.FidelityFX_SDK2_Denoiser_DX12_BACKUP => FidelityFXSDK2DenoiserDX12Records,
            GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12 or GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12_BACKUP => FidelityFXSDK2FrameGenerationDX12Records,
            GameAssetType.FidelityFX_SDK2_Loader_DX12 or GameAssetType.FidelityFX_SDK2_Loader_DX12_BACKUP => FidelityFXSDK2LoaderDX12Records,
            GameAssetType.FidelityFX_SDK2_RadianceCache_DX12 or GameAssetType.FidelityFX_SDK2_RadianceCache_DX12_BACKUP => FidelityFXSDK2RadianceCacheDX12Records,
            GameAssetType.FidelityFX_SDK2_Upscaler_DX12 or GameAssetType.FidelityFX_SDK2_Upscaler_DX12_BACKUP => FidelityFXSDK2UpscalerDX12Records,
            GameAssetType.Streamline_Reflex or GameAssetType.Streamline_Reflex_BACKUP => StreamlineReflexRecords,
            GameAssetType.Streamline_PCL or GameAssetType.Streamline_PCL_BACKUP => StreamlinePCLRecords,
            GameAssetType.Streamline_NvPerf or GameAssetType.Streamline_NvPerf_BACKUP => StreamlineNvPerfRecords,
            GameAssetType.Streamline_NIS or GameAssetType.Streamline_NIS_BACKUP => StreamlineNISRecords,
            GameAssetType.Streamline_Interposer or GameAssetType.Streamline_Interposer_BACKUP => StreamlineInterposerRecords,
            GameAssetType.Streamline_DLSS_G or GameAssetType.Streamline_DLSS_G_BACKUP => StreamlineDLSSGRecords,
            GameAssetType.Streamline_DLSS_D or GameAssetType.Streamline_DLSS_D_BACKUP => StreamlineDLSSDRecords,
            GameAssetType.Streamline_DLSS_NR or GameAssetType.Streamline_DLSS_NR_BACKUP => StreamlineDLSSNRRecords,
            GameAssetType.Streamline_DLSS or GameAssetType.Streamline_DLSS_BACKUP => StreamlineDLSSRecords,
            GameAssetType.Streamline_DirectSR or GameAssetType.Streamline_DirectSR_BACKUP => StreamlineDirectSRRecords,
            GameAssetType.Streamline_DeepDVC or GameAssetType.Streamline_DeepDVC_BACKUP => StreamlineDeepDVCRecords,
            GameAssetType.Streamline_Common or GameAssetType.Streamline_Common_BACKUP => StreamlineCommonRecords,
            GameAssetType.DeepDVC or GameAssetType.DeepDVC_BACKUP => DeepDVCRecords,
            GameAssetType.NvLowLatencyVK or GameAssetType.NvLowLatencyVK_BACKUP => NvLowLatencyVKRecords,
            _ => new ObservableCollection<DLLRecord>(),
        };

        var knownDllList = gameAsset.AssetType switch
        {
            GameAssetType.DLSS or GameAssetType.DLSS_BACKUP => KnownDLLs.DLSS,
            GameAssetType.DLSS_D or GameAssetType.DLSS_D_BACKUP => KnownDLLs.DLSS_D,
            GameAssetType.DLSS_G or GameAssetType.DLSS_G_BACKUP => KnownDLLs.DLSS_G,
            GameAssetType.DLSS_NR or GameAssetType.DLSS_NR_BACKUP => KnownDLLs.DLSS_NR,
            GameAssetType.FSR_31_DX12 or GameAssetType.FSR_31_DX12_BACKUP => KnownDLLs.FSR_31_DX12,
            GameAssetType.FSR_31_VK or GameAssetType.FSR_31_VK_BACKUP => KnownDLLs.FSR_31_VK,
            GameAssetType.XeSS or GameAssetType.XeSS_BACKUP => KnownDLLs.XeSS,
            GameAssetType.XeLL or GameAssetType.XeLL_BACKUP => KnownDLLs.XeLL,
            GameAssetType.XeSS_FG or GameAssetType.XeSS_FG_BACKUP => KnownDLLs.XeSS_FG,
            GameAssetType.XeSS_DX11 or GameAssetType.XeSS_DX11_BACKUP => KnownDLLs.XeSS_DX11,
            GameAssetType.DirectStorage or GameAssetType.DirectStorage_BACKUP => KnownDLLs.DirectStorage,
            GameAssetType.DirectStorageCore or GameAssetType.DirectStorageCore_BACKUP => KnownDLLs.DirectStorageCore,
            GameAssetType.FidelityFX_SDK2_Denoiser_DX12 or GameAssetType.FidelityFX_SDK2_Denoiser_DX12_BACKUP => KnownDLLs.FidelityFX_SDK2_Denoiser_DX12,
            GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12 or GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12_BACKUP => KnownDLLs.FidelityFX_SDK2_FrameGeneration_DX12,
            GameAssetType.FidelityFX_SDK2_Loader_DX12 or GameAssetType.FidelityFX_SDK2_Loader_DX12_BACKUP => KnownDLLs.FidelityFX_SDK2_Loader_DX12,
            GameAssetType.FidelityFX_SDK2_RadianceCache_DX12 or GameAssetType.FidelityFX_SDK2_RadianceCache_DX12_BACKUP => KnownDLLs.FidelityFX_SDK2_RadianceCache_DX12,
            GameAssetType.FidelityFX_SDK2_Upscaler_DX12 or GameAssetType.FidelityFX_SDK2_Upscaler_DX12_BACKUP => KnownDLLs.FidelityFX_SDK2_Upscaler_DX12,
            GameAssetType.Streamline_Reflex or GameAssetType.Streamline_Reflex_BACKUP => KnownDLLs.Streamline_Reflex,
            GameAssetType.Streamline_PCL or GameAssetType.Streamline_PCL_BACKUP => KnownDLLs.Streamline_PCL,
            GameAssetType.Streamline_NvPerf or GameAssetType.Streamline_NvPerf_BACKUP => KnownDLLs.Streamline_NvPerf,
            GameAssetType.Streamline_NIS or GameAssetType.Streamline_NIS_BACKUP => KnownDLLs.Streamline_NIS,
            GameAssetType.Streamline_Interposer or GameAssetType.Streamline_Interposer_BACKUP => KnownDLLs.Streamline_Interposer,
            GameAssetType.Streamline_DLSS_G or GameAssetType.Streamline_DLSS_G_BACKUP => KnownDLLs.Streamline_DLSS_G,
            GameAssetType.Streamline_DLSS_D or GameAssetType.Streamline_DLSS_D_BACKUP => KnownDLLs.Streamline_DLSS_D,
            GameAssetType.Streamline_DLSS_NR or GameAssetType.Streamline_DLSS_NR_BACKUP => KnownDLLs.Streamline_DLSS_NR,
            GameAssetType.Streamline_DLSS or GameAssetType.Streamline_DLSS_BACKUP => KnownDLLs.Streamline_DLSS,
            GameAssetType.Streamline_DirectSR or GameAssetType.Streamline_DirectSR_BACKUP => KnownDLLs.Streamline_DirectSR,
            GameAssetType.Streamline_DeepDVC or GameAssetType.Streamline_DeepDVC_BACKUP => KnownDLLs.Streamline_DeepDVC,
            GameAssetType.Streamline_Common or GameAssetType.Streamline_Common_BACKUP => KnownDLLs.Streamline_Common,
            GameAssetType.DeepDVC or GameAssetType.DeepDVC_BACKUP => KnownDLLs.DeepDVC,
            GameAssetType.NvLowLatencyVK or GameAssetType.NvLowLatencyVK_BACKUP => KnownDLLs.NvLowLatencyVK,
            _ => new List<HashedKnownDLL>(),
        };


        if (recordList.Any(x => gameAsset.Hash.Equals(x.MD5Hash, StringComparison.InvariantCultureIgnoreCase)))
        {
            return true;
        }

        HashedKnownDLL? hashedKnownDLL = null;
        _knownDLLsReadWriterLock.EnterReadLock();
        try
        {
            hashedKnownDLL = knownDllList.FirstOrDefault(x => gameAsset.Hash.Equals(x.Hash, StringComparison.InvariantCultureIgnoreCase));
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath">Path of the DLL you wish to import.</param>
    /// <param name="zippedDllFullName"></param>
    /// <param name="overrideFileName">Override the filename for importing NGX models that are not .dlls yet.</param>
    /// <returns></returns>
    internal DLLImportResult ImportDll(string filePath, string? zippedDllFullName = null, string? overrideFileName = null)
    {
        if (ImportedManifest is null)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, ResourceHelper.GetString("DllManager_ImportFeatureDisabled"));
        }

        var fileName = overrideFileName ?? Path.GetFileName(filePath);

        ObservableCollection<DLLRecord>? recordList = null;
        List<DLLRecord>? importedRecordList = null;
        GameAssetType? gameAssetType = null;

        // NOTE: DLL type
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
        else if (fileName == "nvngx_dlssnr.dll")
        {
            gameAssetType = GameAssetType.DLSS_NR;
            recordList = DLSSNRRecords;
            importedRecordList = ImportedManifest.DLSS_NR;
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
        else if (fileName == "libxess_dx11.dll")
        {
            gameAssetType = GameAssetType.XeSS_DX11;
            recordList = XeSSDX11Records;
            importedRecordList = ImportedManifest.XeSS_DX11;
        }
        else if (fileName == "libxess_fg.dll")
        {
            gameAssetType = GameAssetType.XeSS_FG;
            recordList = XeSSFGRecords;
            importedRecordList = ImportedManifest.XeSS_FG;
        }
        else if (fileName == "dstorage.dll")
        {
            gameAssetType = GameAssetType.DirectStorage;
            recordList = DirectStorageRecords;
            importedRecordList = ImportedManifest.DirectStorage;
        }
        else if (fileName == "dstoragecore.dll")
        {
            gameAssetType = GameAssetType.DirectStorageCore;
            recordList = DirectStorageCoreRecords;
            importedRecordList = ImportedManifest.DirectStorageCore;
        }
        else if (fileName == "amd_fidelityfx_denoiser_dx12.dll")
        {
            gameAssetType = GameAssetType.FidelityFX_SDK2_Denoiser_DX12;
            recordList = FidelityFXSDK2DenoiserDX12Records;
            importedRecordList = ImportedManifest.FidelityFX_SDK2_Denoiser_DX12;
        }
        else if (fileName == "amd_fidelityfx_framegeneration_dx12.dll")
        {
            gameAssetType = GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12;
            recordList = FidelityFXSDK2FrameGenerationDX12Records;
            importedRecordList = ImportedManifest.FidelityFX_SDK2_FrameGeneration_DX12;
        }
        else if (fileName == "amd_fidelityfx_loader_dx12.dll")
        {
            gameAssetType = GameAssetType.FidelityFX_SDK2_Loader_DX12;
            recordList = FidelityFXSDK2LoaderDX12Records;
            importedRecordList = ImportedManifest.FidelityFX_SDK2_Loader_DX12;
        }
        else if (fileName == "amd_fidelityfx_radiancecache_dx12.dll")
        {
            gameAssetType = GameAssetType.FidelityFX_SDK2_RadianceCache_DX12;
            recordList = FidelityFXSDK2RadianceCacheDX12Records;
            importedRecordList = ImportedManifest.FidelityFX_SDK2_RadianceCache_DX12;
        }
        else if (fileName == "amd_fidelityfx_upscaler_dx12.dll")
        {
            gameAssetType = GameAssetType.FidelityFX_SDK2_Upscaler_DX12;
            recordList = FidelityFXSDK2UpscalerDX12Records;
            importedRecordList = ImportedManifest.FidelityFX_SDK2_Upscaler_DX12;
        }
        else if (fileName == "sl.reflex.dll")
        {
            gameAssetType = GameAssetType.Streamline_Reflex;
            recordList = StreamlineReflexRecords;
            importedRecordList = ImportedManifest.Streamline_Reflex;
        }
        else if (fileName == "sl.pcl.dll")
        {
            gameAssetType = GameAssetType.Streamline_PCL;
            recordList = StreamlinePCLRecords;
            importedRecordList = ImportedManifest.Streamline_PCL;
        }
        else if (fileName == "sl.nvperf.dll")
        {
            gameAssetType = GameAssetType.Streamline_NvPerf;
            recordList = StreamlineNvPerfRecords;
            importedRecordList = ImportedManifest.Streamline_NvPerf;
        }
        else if (fileName == "sl.nis.dll")
        {
            gameAssetType = GameAssetType.Streamline_NIS;
            recordList = StreamlineNISRecords;
            importedRecordList = ImportedManifest.Streamline_NIS;
        }
        else if (fileName == "sl.interposer.dll")
        {
            gameAssetType = GameAssetType.Streamline_Interposer;
            recordList = StreamlineInterposerRecords;
            importedRecordList = ImportedManifest.Streamline_Interposer;
        }
        else if (fileName == "sl.dlss_d.dll")
        {
            gameAssetType = GameAssetType.Streamline_DLSS_G;
            recordList = StreamlineDLSSGRecords;
            importedRecordList = ImportedManifest.Streamline_DLSS_G;
        }
        else if (fileName == "sl.dlss_g.dll")
        {
            gameAssetType = GameAssetType.Streamline_DLSS_D;
            recordList = StreamlineDLSSDRecords;
            importedRecordList = ImportedManifest.Streamline_DLSS_D;
        }
        else if (fileName == "sl.dlss_nr.dll")
        {
            gameAssetType = GameAssetType.Streamline_DLSS_NR;
            recordList = StreamlineDLSSNRRecords;
            importedRecordList = ImportedManifest.Streamline_DLSS_NR;
        }
        else if (fileName == "sl.dlss.dll")
        {
            gameAssetType = GameAssetType.Streamline_DLSS;
            recordList = StreamlineDLSSRecords;
            importedRecordList = ImportedManifest.Streamline_DLSS;
        }
        else if (fileName == "sl.directsr.dll")
        {
            gameAssetType = GameAssetType.Streamline_DirectSR;
            recordList = StreamlineDirectSRRecords;
            importedRecordList = ImportedManifest.Streamline_DirectSR;
        }
        else if (fileName == "sl.deepdvc.dll")
        {
            gameAssetType = GameAssetType.Streamline_DeepDVC;
            recordList = StreamlineDeepDVCRecords;
            importedRecordList = ImportedManifest.Streamline_DeepDVC;
        }
        else if (fileName == "sl.common.dll")
        {
            gameAssetType = GameAssetType.Streamline_Common;
            recordList = StreamlineCommonRecords;
            importedRecordList = ImportedManifest.Streamline_Common;
        }
        else if (fileName == "nvngx_deepdvc.dll")
        {
            gameAssetType = GameAssetType.DeepDVC;
            recordList = DeepDVCRecords;
            importedRecordList = ImportedManifest.DeepDVC;
        }
        else if (fileName == "NvLowLatencyVk.dll")
        {
            gameAssetType = GameAssetType.NvLowLatencyVK;
            recordList = NvLowLatencyVKRecords;
            importedRecordList = ImportedManifest.NvLowLatencyVK;
        }


        if (gameAssetType is null || recordList is null || importedRecordList is null)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, ResourceHelper.GetString("DllManager_UnknownTypeDll"));
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        var isTrusted = WinTrust.VerifyEmbeddedSignature(filePath);

        // Don't do anything with untrusted dlls.
        if (Settings.Instance.AllowUntrusted == false && isTrusted == false)
        {
            return DLLImportResult.FromFail(zippedDllFullName ?? filePath, ResourceHelper.GetString("DllManager_UntrustedDll"));
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
                return DLLImportResult.FromSucces(zippedDllFullName ?? filePath, $"{fileName} {ResourceHelper.GetString("DllManager_AlreadyImported")}", false);
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

            var expectedPath = GetExpectedDllFileName(dllRecord, !importingAsDownloadedDll);
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

        // NOTE: DLL type
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
        else if (dllRecord.AssetType == GameAssetType.DLSS_NR)
        {
            recordList = DLSSNRRecords;
            importedRecordList = ImportedManifest?.DLSS_NR;
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
        else if (dllRecord.AssetType == GameAssetType.XeSS_FG)
        {
            recordList = XeSSFGRecords;
            importedRecordList = ImportedManifest?.XeSS_FG;
        }
        else if (dllRecord.AssetType == GameAssetType.XeSS_DX11)
        {
            recordList = XeSSDX11Records;
            importedRecordList = ImportedManifest?.XeSS_DX11;
        }
        else if (dllRecord.AssetType == GameAssetType.XeLL)
        {
            recordList = XeLLRecords;
            importedRecordList = ImportedManifest?.XeLL;
        }
        else if (dllRecord.AssetType == GameAssetType.DirectStorage)
        {
            recordList = DirectStorageRecords;
            importedRecordList = ImportedManifest?.DirectStorage;
        }
        else if (dllRecord.AssetType == GameAssetType.DirectStorageCore)
        {
            recordList = DirectStorageCoreRecords;
            importedRecordList = ImportedManifest?.DirectStorageCore;
        }
        else if (dllRecord.AssetType == GameAssetType.FidelityFX_SDK2_Denoiser_DX12)
        {
            recordList = FidelityFXSDK2DenoiserDX12Records;
            importedRecordList = ImportedManifest?.FidelityFX_SDK2_Denoiser_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12)
        {
            recordList = FidelityFXSDK2FrameGenerationDX12Records;
            importedRecordList = ImportedManifest?.FidelityFX_SDK2_FrameGeneration_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.FidelityFX_SDK2_Loader_DX12)
        {
            recordList = FidelityFXSDK2LoaderDX12Records;
            importedRecordList = ImportedManifest?.FidelityFX_SDK2_Loader_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.FidelityFX_SDK2_RadianceCache_DX12)
        {
            recordList = FidelityFXSDK2RadianceCacheDX12Records;
            importedRecordList = ImportedManifest?.FidelityFX_SDK2_RadianceCache_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.FidelityFX_SDK2_Upscaler_DX12)
        {
            recordList = FidelityFXSDK2UpscalerDX12Records;
            importedRecordList = ImportedManifest?.FidelityFX_SDK2_Upscaler_DX12;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_Reflex)
        {
            recordList = StreamlineReflexRecords;
            importedRecordList = ImportedManifest?.Streamline_Reflex;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_PCL)
        {
            recordList = StreamlinePCLRecords;
            importedRecordList = ImportedManifest?.Streamline_PCL;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_NvPerf)
        {
            recordList = StreamlineNvPerfRecords;
            importedRecordList = ImportedManifest?.Streamline_NvPerf;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_NIS)
        {
            recordList = StreamlineNISRecords;
            importedRecordList = ImportedManifest?.Streamline_NIS;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_Interposer)
        {
            recordList = StreamlineInterposerRecords;
            importedRecordList = ImportedManifest?.Streamline_Interposer;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DLSS_G)
        {
            recordList = StreamlineDLSSGRecords;
            importedRecordList = ImportedManifest?.Streamline_DLSS_G;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DLSS_D)
        {
            recordList = StreamlineDLSSDRecords;
            importedRecordList = ImportedManifest?.Streamline_DLSS_D;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DLSS_NR)
        {
            recordList = StreamlineDLSSNRRecords;
            importedRecordList = ImportedManifest?.Streamline_DLSS_NR;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DLSS)
        {
            recordList = StreamlineDLSSRecords;
            importedRecordList = ImportedManifest?.Streamline_DLSS;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DirectSR)
        {
            recordList = StreamlineDirectSRRecords;
            importedRecordList = ImportedManifest?.Streamline_DirectSR;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_DeepDVC)
        {
            recordList = StreamlineDeepDVCRecords;
            importedRecordList = ImportedManifest?.Streamline_DeepDVC;
        }
        else if (dllRecord.AssetType == GameAssetType.Streamline_Common)
        {
            recordList = StreamlineCommonRecords;
            importedRecordList = ImportedManifest?.Streamline_Common;
        }
        else if (dllRecord.AssetType == GameAssetType.DeepDVC)
        {
            recordList = DeepDVCRecords;
            importedRecordList = ImportedManifest?.DeepDVC;
        }
        else if (dllRecord.AssetType == GameAssetType.NvLowLatencyVK)
        {
            recordList = NvLowLatencyVKRecords;
            importedRecordList = ImportedManifest?.NvLowLatencyVK;
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
        // NOTE: DLL type
        return gameAssetType switch
        {
            GameAssetType.DLSS => "nvngx_dlss.dll",
            GameAssetType.DLSS_G => "nvngx_dlssg.dll",
            GameAssetType.DLSS_D => "nvngx_dlssd.dll",
            GameAssetType.DLSS_NR => "nvngx_dlssnr.dll",
            GameAssetType.FSR_31_DX12 => "amd_fidelityfx_dx12.dll",
            GameAssetType.FSR_31_VK => "amd_fidelityfx_vk.dll",
            GameAssetType.XeSS => "libxess.dll",
            GameAssetType.XeSS_FG => "libxess_fg.dll",
            GameAssetType.XeLL => "libxell.dll",
            GameAssetType.XeSS_DX11 => "libxess_dx11.dll",
            GameAssetType.DirectStorage => "dstorage.dll",
            GameAssetType.DirectStorageCore => "dstoragecore.dll",
            GameAssetType.FidelityFX_SDK2_Denoiser_DX12 => "amd_fidelityfx_denoiser_dx12.dll",
            GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12 => "amd_fidelityfx_framegeneration_dx12.dll",
            GameAssetType.FidelityFX_SDK2_Loader_DX12 => "amd_fidelityfx_loader_dx12.dll",
            GameAssetType.FidelityFX_SDK2_RadianceCache_DX12 => "amd_fidelityfx_radiancecache_dx12.dll",
            GameAssetType.FidelityFX_SDK2_Upscaler_DX12 => "amd_fidelityfx_upscaler_dx12.dll",
            GameAssetType.Streamline_Reflex => "sl.reflex.dll",
            GameAssetType.Streamline_PCL => "sl.pcl.dll",
            GameAssetType.Streamline_NvPerf => "sl.nvperf.dll",
            GameAssetType.Streamline_NIS => "sl.nis.dll",
            GameAssetType.Streamline_Interposer => "sl.interposer.dll",
            GameAssetType.Streamline_DLSS_G => "sl.dlss_d.dll",
            GameAssetType.Streamline_DLSS_D => "sl.dlss_g.dll",
            GameAssetType.Streamline_DLSS_NR => "sl.dlss_nr.dll",
            GameAssetType.Streamline_DLSS => "sl.dlss.dll",
            GameAssetType.Streamline_DirectSR => "sl.directsr.dll",
            GameAssetType.Streamline_DeepDVC => "sl.deepdvc.dll",
            GameAssetType.Streamline_Common => "sl.common.dll",
            GameAssetType.DeepDVC => "nvngx_deepdvc.dll",
            GameAssetType.NvLowLatencyVK => "NvLowLatencyVk.dll",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// This handles extracting of the DLL from both downloaded and imported zips (when imported matches the hash of one that could be downloaded)
    /// </summary>
    /// <param name="zipArchive"></param>
    /// <param name="dllRecord"></param>
    /// <exception cref="Exception"></exception>
    internal static void HandleExtractFromZip(ZipArchive zipArchive, DLLRecord dllRecord)
    {
        if (dllRecord.LocalRecord is null)
        {
            throw new Exception("LocalRecord was null when attempting to extract dll from zip.");
        }

        var dllName = DLLManager.DllNameForGameAssetType(dllRecord.AssetType);
        var entry = zipArchive.Entries.Single(x => x.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new Exception("Could not find dll in zip.");
        }
        else
        {
            Storage.CreateDirectoryForFileIfNotExists(dllRecord.LocalRecord.ExpectedPath);
            entry.ExtractToFile(dllRecord.LocalRecord.ExpectedPath, true);
        }
    }

}
