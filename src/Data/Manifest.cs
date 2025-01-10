﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data;

internal class Manifest
{
    [JsonPropertyName("dlss")]
    public List<DLLRecord> DLSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("dlss_d")]
    public List<DLLRecord> DLSS_D { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("dlss_g")]
    public List<DLLRecord> DLSS_G { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("fsr_31_dx12")]
    public List<DLLRecord> FSR_31_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("fsr_31_vk")]
    public List<DLLRecord> FSR_31_VK { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("xess")]
    public List<DLLRecord> XeSS { get; set; } = new List<DLLRecord>();
}
