using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.DLSS;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;

// One selectable DLL version inside a batch picker row. A null Record is the
// "Don't change" sentinel that leaves this row's DLL untouched.
public class BatchDllOption
{
    public string DisplayName { get; init; } = string.Empty;
    public DLLRecord? Record { get; init; }
}

// A single type row (DLSS, FSR_31_VK, ...). Holds its own DLL-version options and,
// for the three preset-capable types, its own preset options. "Don't change" is
// represented by presence of the sentinel selection, never by a uint value — so
// preset 0 ("Default") stays a real, selectable action.
public partial class BatchDllRowModel : ObservableObject
{
    public GameAssetType Type { get; }
    public string TypeName { get; }

    // TypeName is only a visual label in the row Grid, so the combos would otherwise
    // reach Narrator unnamed. Both combos in a row need distinct names.
    public string DllAutomationName { get; }
    public string PresetAutomationName { get; }

    // True for DLSS / DLSS_D / DLSS_G.
    public bool HasPreset { get; }

    // HasPreset && NVAPI supported. When false the preset combo shows only the
    // sentinel and is disabled.
    public bool PresetEnabled { get; }

    // DllOptions[0] is always the sentinel (Record == null).
    public List<BatchDllOption> DllOptions { get; }

    // PresetOptions[0] is the sentinel when HasPreset; empty otherwise.
    public List<PresetOption> PresetOptions { get; }

    readonly PresetOption? _presetSentinel;

    [ObservableProperty]
    public partial BatchDllOption? SelectedDllOption { get; set; }

    [ObservableProperty]
    public partial PresetOption? SelectedPreset { get; set; }

    public BatchDllRowModel(GameAssetType type, string typeName, List<BatchDllOption> dllOptions, PresetOption? presetSentinel, IReadOnlyList<PresetOption>? presetOptions, bool presetEnabled)
    {
        Type = type;
        TypeName = typeName;
        DllAutomationName = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_VersionForTypeTemplate", typeName);
        PresetAutomationName = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_PresetForTypeTemplate", typeName);
        DllOptions = dllOptions;
        SelectedDllOption = dllOptions.FirstOrDefault(); // sentinel

        HasPreset = presetSentinel is not null;
        PresetEnabled = presetEnabled;
        _presetSentinel = presetSentinel;

        var options = new List<PresetOption>();
        if (presetSentinel is not null)
        {
            options.Add(presetSentinel);
            if (presetOptions is not null)
            {
                options.AddRange(presetOptions);
            }
        }
        PresetOptions = options;
        SelectedPreset = _presetSentinel;
    }

    public bool HasDllAction => SelectedDllOption?.Record is not null;

    public bool HasPresetAction =>
        HasPreset
        && SelectedPreset is not null
        && ReferenceEquals(SelectedPreset, _presetSentinel) == false;
}
