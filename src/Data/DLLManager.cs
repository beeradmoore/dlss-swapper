using System;
using System.Collections.ObjectModel;
using System.IO;

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

    internal void LoadFromManifest()
    {

    }


    // Previously: UpdateDLSSRecordsList
    internal void UpdateDLLRecordLists(Manifest manifest)
    {
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
        };
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
        var recordType = dllRecord.AssetType switch
        {
            GameAssetType.DLSS => "dlss",
            GameAssetType.DLSS_G => "dlss_g",
            GameAssetType.DLSS_D => "dlss_d",
            GameAssetType.FSR_31_DX12 => "fsr_31_dx12",
            GameAssetType.FSR_31_VK => "fsr_31_vk",
            GameAssetType.XeSS => "xess",
            GameAssetType.XeLL => "xell",
            GameAssetType.XeSS_FG => "xess_fg",
            _ => string.Empty,
        };

        if (recordType == string.Empty)
        {
            return string.Empty;
        }

#if PORTABLE
        var zipPath = Path.Combine("StoredData", (isImportedRecord ? $"imported_{recordType}_zip" : $"{recordType}_zip"));
#else
        var zipPath = Path.Combine(Storage.GetStorageFolder(), (isImportedRecord ? $"imported_{recordType}_zip" : $"{recordType}_zip"));
#endif
        return zipPath;
    }

    internal string GetExpectedPath(DLLRecord dllRecord, bool isImportedRecord = false)
    {
        var zipPath = GetExpectedZipPath(dllRecord, isImportedRecord);
        if (string.IsNullOrEmpty(zipPath))
        {
            return string.Empty;
        }
        return Path.Combine(zipPath, $"{dllRecord.Version}_{dllRecord.MD5Hash}.zip");
    }

    internal void LoadLocalRecordFromDLSSRecord(DLLRecord dllRecord, bool isImportedRecord = false)
    {
        var expectedPath = GetExpectedPath(dllRecord, isImportedRecord);

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
}
