using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace DLSS_Swapper.Helpers.FSR31;

internal class FSR31Helper
{
    // Define the LoadLibrary and GetProcAddress functions
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    // Define a delegate for the ffxQuery function
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate FfxApiReturnCodes ffxQueryDelegate(IntPtr context, ref QueryDescGetVersions header);

    public static List<string?> GetVersions(string dllPath)
    {
        if (Path.Exists(dllPath) == false)
        {
            return new List<string?>();
        }
        Console.WriteLine($"AMDFidelityFXAPI - Loading {dllPath}");
        var hModule = LoadLibrary(dllPath);
        if (hModule == IntPtr.Zero)
        {
            throw new Exception("Failed to load DLL");
        }

        try
        {
            var pAddressOfFunctionToCall = GetProcAddress(hModule, "ffxQuery");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                throw new Exception("Failed to get function address");
            }

            var ffxQuery = Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(ffxQueryDelegate)) as ffxQueryDelegate;
            if (ffxQuery is null)
            {
                throw new Exception("Failed to get function delegate");
            }

            var versionQuery = new QueryDescGetVersions();

            //versionQuery.createDescType = FFX_API_CREATE_CONTEXT_DESC_TYPE_UPSCALE;
            versionQuery.createDescType = FxxConsts.FFX_API_CREATE_CONTEXT_DESC_TYPE_UPSCALE;

            // versionQuery.device = GetDX12Device(); // only for DirectX 12 applications
            versionQuery.device = IntPtr.Zero;

            // uint64_t versionCount = 0;
            UInt64 versionCount = 0;
            versionQuery.outputCount = Marshal.AllocHGlobal(sizeof(UInt64));
            Marshal.WriteInt64(versionQuery.outputCount, (UInt32)versionCount);

            Console.WriteLine("AMDFidelityFXAPI - Reading version count");
            // get number of versions for allocation
            // ffxQuery(IntPtr.Zero, &versionQuery.header);
            var returnCode = ffxQuery(IntPtr.Zero, ref versionQuery);
            Console.WriteLine($"AMDFidelityFXAPI - returnCode: {returnCode}");

            if (returnCode != FfxApiReturnCodes.FFX_API_RETURN_OK)
            {
                throw new Exception($"Failed to get version count. Return code: {returnCode}");
            }

            versionCount = (UInt64)Marshal.ReadInt64(versionQuery.outputCount);
            Console.WriteLine($"AMDFidelityFXAPI - versionCount: {versionCount}");

            if (versionCount > 0)
            {
                var versionCountInt = (int)versionCount;

                //std::vector <const char*> versionNames;
                //std::vector<uint64_t> versionIds;
                //m_FsrVersionIds.resize(versionCount);
                //versionNames.resize(versionCount);
                var versionNames = new List<string?>(versionCountInt);
                var versionIds = new List<UInt64>(versionCountInt);

                var versionNamesPtrs = new IntPtr[versionCountInt];
                var versionIdsPtrs = new IntPtr[versionCountInt];

                try
                {
                    for (var i = 0; i < versionCountInt; i++)
                    {
                        versionNames.Add(null);
                        versionIds.Add(0);
                    }

                    //versionQuery.versionIds = versionIds.data();
                    //versionQuery.versionNames = versionNames.data();

                    versionQuery.versionIds = Marshal.AllocHGlobal(sizeof(UInt64) * versionCountInt);
                    versionQuery.versionNames = Marshal.AllocHGlobal(IntPtr.Size * versionCountInt);

                    for (var i = 0; i < versionCountInt; i++)
                    {
                        //versionIdsPtrs[i] = Marshal.AllocHGlobal(sizeof(UInt64));
                        //Marshal.WriteInt64(versionIdsPtrs[i], (long)versionIds[i]);
                        //Marshal.WriteIntPtr(versionQuery.versionIds, i * IntPtr.Size, versionIdsPtrs[i]);

                        Marshal.WriteInt64(versionQuery.versionIds, i * sizeof(UInt64), 0L);

                        versionNamesPtrs[i] = Marshal.StringToHGlobalAnsi(versionNames[i]);
                        Marshal.WriteIntPtr(versionQuery.versionNames, i * IntPtr.Size, versionNamesPtrs[i]);
                    }

                    // fill version ids and names arrays.
                    //ffxQuery(nullptr, &versionQuery.header);
                    returnCode = ffxQuery(IntPtr.Zero, ref versionQuery);
                    Console.WriteLine($"AMDFidelityFXAPI - returnCode: {returnCode}");

                    if (returnCode != FfxApiReturnCodes.FFX_API_RETURN_OK)
                    {
                        throw new Exception($"Failed to get version count. Return code: {returnCode}");
                    }

                    for (var i = 0; i < versionCountInt; i++)
                    {
                        //Marshal.ReadIntPtr(versionQuery.versionIds, i)
                        versionIds[i] = (UInt64)Marshal.ReadInt64(versionQuery.versionIds, i * sizeof(UInt64));
                        versionNames[i] = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(versionQuery.versionNames, i * IntPtr.Size));
                    }

                    Console.WriteLine("AMDFidelityFXAPI - Version Names and IDs:");

                    for (var i = 0; i < versionCountInt; i++)
                    {
                        //FFX_SDK_MAKE_VERSION( major, minor, patch ) ( ( major << 22 ) | ( minor << 12 ) | patch )
                        var major = (versionIds[i] >> 22) & 0x3FF;
                        var minor = (versionIds[i] >> 12) & 0x3FF;
                        var patch = versionIds[i] & 0xFFF;

                        Console.WriteLine($"ID: {versionIds[i]}, {major}.{minor}.{patch}, Name: {versionNames[i]}");
                    }

                    return versionNames;
                }
                finally
                {
                    foreach (var ptr in versionIdsPtrs)
                    {
                        //  Marshal.FreeHGlobal(ptr);
                    }
                    foreach (var ptr in versionNamesPtrs)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                    Marshal.FreeHGlobal(versionQuery.versionIds);
                    Marshal.FreeHGlobal(versionQuery.versionNames);
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "AMDFidelityFXAPI");
        }
        finally
        {
            FreeLibrary(hModule);
        }

        return new List<string?>();
    }

    public static string GetLatestVersion(string dllPath)
    {
        // We really assume these are coming in with the latest version first.
        var versions = GetVersions(dllPath);
        foreach (var version in versions)
        {
            if (string.IsNullOrWhiteSpace(version) == false)
            {
                return version;
            }
        }
        return string.Empty;
    }
}
