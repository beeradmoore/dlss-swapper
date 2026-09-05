using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class DLLSets
{
    [JsonPropertyName(DLLSet.DLSS)]
    public List<DLLSet> DLSS { get; set; } = new List<DLLSet>();

    [JsonPropertyName(DLLSet.FidelityFX_SDK1)]
    public List<DLLSet> FidelityFX_SDK1 { get; set; } = new List<DLLSet>();

    [JsonPropertyName(DLLSet.FidelityFX_SDK2)]
    public List<DLLSet> FidelityFX_SDK2 { get; set; } = new List<DLLSet>();

    [JsonPropertyName(DLLSet.XeSS)]
    public List<DLLSet> XeSS { get; set; } = new List<DLLSet>();

    [JsonPropertyName(DLLSet.DirectStorage)]
    public List<DLLSet> DirectStorage { get; set; } = new List<DLLSet>();

    [JsonPropertyName(DLLSet.Streamline)]
    public List<DLLSet> Streamline { get; set; } = new List<DLLSet>();
}
