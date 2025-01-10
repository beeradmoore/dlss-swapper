using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmHelpers;

namespace DLSS_Swapper.Data;

internal class DLLManager
{
    public static DLLManager Instance { get; private set; } = new DLLManager();

    public ObservableRangeCollection<DLLRecord> DLSSRecords { get; } = new ObservableRangeCollection<DLLRecord>();
    public ObservableRangeCollection<DLLRecord> DLSSGRecords { get; } = new ObservableRangeCollection<DLLRecord>();
    public ObservableRangeCollection<DLLRecord> DLSSDRecords { get; } = new ObservableRangeCollection<DLLRecord>();
    public ObservableRangeCollection<DLLRecord> FSR31DX12Records { get; } = new ObservableRangeCollection<DLLRecord>();
    public ObservableRangeCollection<DLLRecord> FSR31VKRecords { get; } = new ObservableRangeCollection<DLLRecord>();
    public ObservableRangeCollection<DLLRecord> XeSSRecords { get; } = new ObservableRangeCollection<DLLRecord>();

    internal void LoadFromManifest()
    {

    }


    // Previously: UpdateDLSSRecordsList
    internal void UpdateDLLRecordLists(Manifest manifest)
    {
        // TODO: Only change changed items

        manifest.DLSS.Sort();
        foreach (var dllRecord in manifest.DLSS)
        {
            dllRecord.AssetType = GameAssetType.DLSS;
        }
        DLSSRecords.Clear();
        DLSSRecords.AddRange(manifest.DLSS);



        manifest.DLSS_G.Sort();
        foreach (var dllRecord in manifest.DLSS_G)
        {
            dllRecord.AssetType = GameAssetType.DLSS_G;
        }
        DLSSGRecords.Clear();
        DLSSGRecords.AddRange(manifest.DLSS_G);


        manifest.DLSS_D.Sort();
        foreach (var dllRecord in manifest.DLSS_D)
        {
            dllRecord.AssetType = GameAssetType.DLSS_D;
        }
        DLSSDRecords.Clear();
        DLSSDRecords.AddRange(manifest.DLSS_D);


        manifest.FSR_31_DX12.Sort();
        foreach (var dllRecord in manifest.FSR_31_DX12)
        {
            dllRecord.AssetType = GameAssetType.FSR_31_DX12;
        }
        FSR31DX12Records.Clear();
        FSR31DX12Records.AddRange(manifest.FSR_31_DX12);


        manifest.FSR_31_VK.Sort();
        foreach (var dllRecord in manifest.FSR_31_VK)
        {
            dllRecord.AssetType = GameAssetType.FSR_31_VK;
        }
        FSR31VKRecords.Clear();
        FSR31VKRecords.AddRange(manifest.FSR_31_VK);


        manifest.XeSS.Sort();
        foreach (var dllRecord in manifest.XeSS)
        {
            dllRecord.AssetType = GameAssetType.XeSS;
        }
        XeSSRecords.Clear();
        XeSSRecords.AddRange(manifest.XeSS);
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

}
