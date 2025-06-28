using System;
using System.Runtime.InteropServices;

namespace DLSS_Swapper.Helpers.FSR31;

[StructLayout(LayoutKind.Sequential)]
public struct ffxApiHeader
{
    public UInt64 type;  ///< The structure type. Must always be set to the corresponding value for any structure (found nearby with a similar name).
    public IntPtr pNext; ///< Pointer to next structure, used for optional parameters and extensions. Can be null.
}


[StructLayout(LayoutKind.Sequential)]
public struct QueryDescGetVersions
{
    public ffxApiHeader header;
    public UInt64 createDescType;    ///< Create description for the effect whose versions should be enumerated.
    public IntPtr device;               ///< For DX12: pointer to ID3D12Device.
    public IntPtr outputCount;      ///< Input capacity of id and name arrays. Output number of returned versions. If initially zero, output is number of available versions.
    public IntPtr versionIds;       ///< Output array of version ids to be used as version overrides. If null, only names and count are returned.
    public IntPtr versionNames;  ///< Output array of version names for display. If null, only ids and count are returned. If both this and versionIds are null, only count is returned.
    //public byte[]? versionNames;  ///< Output array of version names for display. If null, only ids and count are returned. If both this and versionIds are null, only count is returned.

    public QueryDescGetVersions()
    {
        header = new ffxApiHeader()
        {
            type = FxxConsts.FFX_API_QUERY_DESC_TYPE_GET_VERSIONS,
        };
    }
};


public enum FfxApiReturnCodes
{
    FFX_API_RETURN_OK = 0, ///< The oparation was successful.
    FFX_API_RETURN_ERROR = 1, ///< An error occurred that is not further specified.
    FFX_API_RETURN_ERROR_UNKNOWN_DESCTYPE = 2, ///< The structure type given was not recognized for the function or context with which it was used. This is likely a programming error.
    FFX_API_RETURN_ERROR_RUNTIME_ERROR = 3, ///< The underlying runtime (e.g. D3D12, Vulkan) or effect returned an error code.
    FFX_API_RETURN_NO_PROVIDER = 4, ///< No provider was found for the given structure type. This is likely a programming error.
    FFX_API_RETURN_ERROR_MEMORY = 5, ///< A memory allocation failed.
    FFX_API_RETURN_ERROR_PARAMETER = 6, ///< A parameter was invalid, e.g. a null pointer, empty resource or out-of-bounds enum value.
};


public class FxxConsts
{
    public const UInt64 FFX_API_CREATE_CONTEXT_DESC_TYPE_UPSCALE = 0x00010000u;
    public const UInt64 FFX_API_QUERY_DESC_TYPE_GET_VERSIONS = 4u;
}
