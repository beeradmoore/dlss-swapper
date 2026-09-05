using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class DLLSet
{
    public const string DLSS = "dlss";
    public const string Streamline = "streamline";
    public const string DirectStorage = "directstorage";
    public const string FidelityFX_SDK1 = "fidelityfx_sdk1";
    public const string FidelityFX_SDK2 = "fidelityfx_sdk2";
    public const string XeSS = "xess";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("set_hash")]
    public string SetHash { get; set; } = string.Empty;

    [JsonPropertyName("dll_records")]
    public Dictionary<string, string> DLLRecords { get; set; } = new Dictionary<string, string>();
}
