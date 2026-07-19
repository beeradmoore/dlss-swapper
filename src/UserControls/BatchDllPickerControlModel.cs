using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.DLSS;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;

public partial class BatchDllPickerControlModel : ObservableObject
{
    readonly WeakReference<EasyContentDialog> _parentDialogWeakReference;

    public List<BatchDllRowModel> DlssRows { get; }
    public List<BatchDllRowModel> FsrRows { get; }
    public List<BatchDllRowModel> XeSSRows { get; }

    public bool HasFsrRows => FsrRows.Count > 0;
    public bool HasXeSSRows => XeSSRows.Count > 0;

    // Drives the explanation shown when every preset combo is disabled. Without it
    // the user just sees three greyed-out dropdowns and no reason why.
    public bool PresetsUnsupported { get; }

    public BatchDllPickerControlModelTranslationProperties TranslationProperties { get; } = new BatchDllPickerControlModelTranslationProperties();

    public BatchDllPickerControlModel(EasyContentDialog parentDialog)
    {
        _parentDialogWeakReference = new WeakReference<EasyContentDialog>(parentDialog);
        parentDialog.IsPrimaryButtonEnabled = false;

        var noChangeText = ResourceHelper.GetString("GamesPage_Batch_NoChange");
        var presetSupported = NVAPIHelper.Instance.IsSupported;
        PresetsUnsupported = presetSupported == false;

        // NOTE: DLL type
        // Preset rows are always shown (a preset is useful even with no downloaded
        // DLL). Each preset type uses its OWN option list.
        DlssRows = new List<BatchDllRowModel>
        {
            BuildPresetRow(GameAssetType.DLSS, noChangeText, NVAPIHelper.Instance.DlssPresetOptions, presetSupported),
            BuildPresetRow(GameAssetType.DLSS_D, noChangeText, NVAPIHelper.Instance.DlssDPresetOptions, presetSupported),
            BuildPresetRow(GameAssetType.DLSS_G, noChangeText, NVAPIHelper.Instance.DlssGPresetOptions, presetSupported),
        };

        // DLL-only rows only appear when at least one DLL version is available.
        FsrRows = BuildDllOnlyRows(noChangeText, new[] { GameAssetType.FSR_31_DX12, GameAssetType.FSR_31_VK });
        XeSSRows = BuildDllOnlyRows(noChangeText, new[] { GameAssetType.XeSS, GameAssetType.XeSS_FG, GameAssetType.XeSS_DX11, GameAssetType.XeLL });

        foreach (var row in AllRows())
        {
            row.PropertyChanged += Row_PropertyChanged;
        }
    }

    BatchDllRowModel BuildPresetRow(GameAssetType type, string noChangeText, IReadOnlyList<PresetOption> presetOptions, bool presetEnabled)
    {
        var options = BuildDllOptions(type, noChangeText);
        // Sentinel value 0xFFFFFFFF is not a real preset (PresetOption.UpdateNameFromTranslation
        // leaves unknown values untouched), so it never collides with Default (0).
        var sentinel = new PresetOption(noChangeText, 0xFFFFFFFF);
        return new BatchDllRowModel(type, DLLManager.Instance.GetAssetTypeName(type), options, sentinel, presetOptions, presetEnabled);
    }

    List<BatchDllRowModel> BuildDllOnlyRows(string noChangeText, GameAssetType[] types)
    {
        var rows = new List<BatchDllRowModel>();
        foreach (var type in types)
        {
            var options = BuildDllOptions(type, noChangeText);
            // options[0] is the sentinel; a real version exists only if count > 1.
            if (options.Count <= 1)
            {
                continue;
            }
            rows.Add(new BatchDllRowModel(type, DLLManager.Instance.GetAssetTypeName(type), options, null, null, false));
        }
        return rows;
    }

    static List<BatchDllOption> BuildDllOptions(GameAssetType type, string noChangeText)
    {
        // NOTE: DLL type
        var records = type switch
        {
            GameAssetType.DLSS => DLLManager.Instance.DLSSRecords.ToList(),
            GameAssetType.DLSS_G => DLLManager.Instance.DLSSGRecords.ToList(),
            GameAssetType.DLSS_D => DLLManager.Instance.DLSSDRecords.ToList(),
            GameAssetType.FSR_31_DX12 => DLLManager.Instance.FSR31DX12Records.ToList(),
            GameAssetType.FSR_31_VK => DLLManager.Instance.FSR31VKRecords.ToList(),
            GameAssetType.XeSS => DLLManager.Instance.XeSSRecords.ToList(),
            GameAssetType.XeSS_DX11 => DLLManager.Instance.XeSSDX11Records.ToList(),
            GameAssetType.XeSS_FG => DLLManager.Instance.XeSSFGRecords.ToList(),
            GameAssetType.XeLL => DLLManager.Instance.XeLLRecords.ToList(),
            _ => new List<DLLRecord>(),
        };

        if (Settings.Instance.OnlyShowDownloadedDlls == true)
        {
            records.RemoveAll(x => x.LocalRecord?.IsDownloaded != true);
        }

        if (Settings.Instance.AllowDebugDlls == false)
        {
            records.RemoveAll(x => x.IsDevFile == true);
        }

        // Only records with a LocalRecord can ever be applied (the executor needs
        // LocalRecord before it can download). Dropping the rest here keeps the
        // PlannedDllActions invariant: every planned record has a LocalRecord.
        records.RemoveAll(x => x.LocalRecord is null);

        var options = new List<BatchDllOption>
        {
            new BatchDllOption { DisplayName = noChangeText, Record = null },
        };
        options.AddRange(records.Select(r => new BatchDllOption { DisplayName = r.DisplayName, Record = r }));
        return options;
    }

    void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BatchDllRowModel.SelectedDllOption) || e.PropertyName == nameof(BatchDllRowModel.SelectedPreset))
        {
            if (_parentDialogWeakReference.TryGetTarget(out var dialog) == true)
            {
                dialog.IsPrimaryButtonEnabled = AllRows().Any(r => r.HasDllAction || r.HasPresetAction);
            }
        }
    }

    IEnumerable<BatchDllRowModel> AllRows() => DlssRows.Concat(FsrRows).Concat(XeSSRows);

    // What the picker decides — never *who* it applies to.
    public List<(GameAssetType Type, DLLRecord Record)> PlannedDllActions =>
        AllRows()
            .Where(r => r.HasDllAction)
            .Select(r => (r.Type, r.SelectedDllOption!.Record!))
            .ToList();

    public List<(GameAssetType Type, PresetOption Preset)> PlannedPresetActions =>
        AllRows()
            .Where(r => r.HasPresetAction)
            .Select(r => (r.Type, r.SelectedPreset!))
            .ToList();
}
