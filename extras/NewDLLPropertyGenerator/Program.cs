using NewDLLPropertyGenerator;

Console.Clear();

var newProperties = new List<PropertyDetails>();

newProperties.Add(new PropertyDetails("DLSS", "DLSS", "dlss", "nvngx_dlss.dll"));
newProperties.Add(new PropertyDetails("DLSS_D", "DLSS Ray Reconstruction", "dlss_d", "nvngx_dlssd.dll"));
newProperties.Add(new PropertyDetails("DLSS_G", "DLSS Frame Generation", "dlss_g", "nvngx_dlssg.dll"));
newProperties.Add(new PropertyDetails("DLSS_NR", "DLSS Neural Rendering", "dlss_nr", "nvngx_dlssnr.dll"));

newProperties.Add(new PropertyDetails("FSR_31_DX12", "XXXX", "fsr_31_dx12", "amd_fidelityfx_dx12.dll"));
newProperties.Add(new PropertyDetails("FSR_31_VK", "XXXX", "fsr_31_vk", "amd_fidelityfx_vk.dll"));

newProperties.Add(new PropertyDetails("XeSS", "XXXX", "xess", "libxess.dll"));
newProperties.Add(new PropertyDetails("XeLL", "XXXX", "xell", "libxell.dll"));
newProperties.Add(new PropertyDetails("XeSS_FG", "XXXX", "xess_fg", "libxess_fg.dll"));
newProperties.Add(new PropertyDetails("XeSS_DX11", "XXXX", "xess_dx11", "libxess_dx11.dll"));


newProperties.Add(new PropertyDetails("DirectStorage", "XXXX", "directstorage", "dstorage.dll"));
newProperties.Add(new PropertyDetails("DirectStorageCore", "XXXX", "directstorage_core", "dstoragecore.dll"));

newProperties.Add(new PropertyDetails("FidelityFX_SDK2_Denoiser_DX12", "XXXX", "fidelityfx_sdk2_denoiser_dx12", "amd_fidelityfx_denoiser_dx12.dll"));
newProperties.Add(new PropertyDetails("FidelityFX_SDK2_FrameGeneration_DX12", "XXXX", "fidelityfx_sdk2_framegeneration_dx12", "amd_fidelityfx_framegeneration_dx12.dll"));
newProperties.Add(new PropertyDetails("FidelityFX_SDK2_Loader_DX12", "XXXX", "fidelityfx_sdk2_loader_dx12", "amd_fidelityfx_loader_dx12.dll"));
newProperties.Add(new PropertyDetails("FidelityFX_SDK2_RadianceCache_DX12", "XXXX", "fidelityfx_sdk2_radiancecache_dx12", "amd_fidelityfx_radiancecache_dx12.dll"));
newProperties.Add(new PropertyDetails("FidelityFX_SDK2_Upscaler_DX12", "XXXX", "fidelityfx_sdk2_upscaler_dx12", "amd_fidelityfx_upscaler_dx12.dll"));

newProperties.Add(new PropertyDetails("Streamline_Reflex", "XXXX", "sl_reflex", "sl.reflex.dll"));
newProperties.Add(new PropertyDetails("Streamline_PCL", "XXXX", "sl_pcl", "sl.pcl.dll"));
newProperties.Add(new PropertyDetails("Streamline_NvPerf", "XXXX", "sl_nvperf", "sl.nvperf.dll"));
newProperties.Add(new PropertyDetails("Streamline_NIS", "XXXX", "sl_nis", "sl.nis.dll"));
newProperties.Add(new PropertyDetails("Streamline_Interposer", "XXXX", "sl_interposer", "sl.interposer.dll"));
newProperties.Add(new PropertyDetails("Streamline_DLSS_G", "XXXX", "sl_dlss_g", "sl.dlss_d.dll"));
newProperties.Add(new PropertyDetails("Streamline_DLSS_D", "XXXX", "sl_dlss_d", "sl.dlss_g.dll"));
newProperties.Add(new PropertyDetails("Streamline_DLSS_NR", "XXXX", "sl_dlss_nr", "sl.dlss_nr.dll"));
newProperties.Add(new PropertyDetails("Streamline_DLSS", "XXXX", "sl_dlss", "sl.dlss.dll"));
newProperties.Add(new PropertyDetails("Streamline_DirectSR", "XXXX", "sl_directsr", "sl.directsr.dll"));
newProperties.Add(new PropertyDetails("Streamline_DeepDVC", "XXXX", "sl_deepdvc", "sl.deepdvc.dll"));
newProperties.Add(new PropertyDetails("Streamline_Common", "XXXX", "sl_common", "sl.common.dll"));
newProperties.Add(new PropertyDetails("DeepDVC", "XXXX", "deepdvc", "nvngx_deepdvc.dll"));
newProperties.Add(new PropertyDetails("NvLowLatencyVK", "XXXX", "nvlowlatencyvk", "NvLowLatencyVk.dll"));


var codeRenderer = new CodeRenderer(newProperties);
codeRenderer.RenderAll();
