
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using static DLSS_Swapper.Data.UbisoftConnect.UbisoftConnectLibrary;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DLSS_Swapper.Helpers.Utils;
public static class UbisoftConnectUtils
{
    public static Dictionary<int, UbisoftGameRegistryRecord> CreateInstalledTitlesDictionary(IEnumerable<LogicalDriveState> drives)
    {
        var installedTitles = new Dictionary<int, UbisoftGameRegistryRecord>();

        using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        using (var ubisoftConnectInstallsKey = hklm.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher\Installs"))
        {
            // if ubisoftConnectRegistryKey is null then Ubisoft is not installed .
            if (ubisoftConnectInstallsKey is null)
            {
                throw new Exception("CouldNotDetectUbisoftConnectInstallKey");
            }

            var subKeyNames = ubisoftConnectInstallsKey.GetSubKeyNames();
            foreach (var subKeyName in subKeyNames)
            {
                // Only use the subKeyName that is a number (which is the installId.
                if (Int32.TryParse(subKeyName, out var installId))
                {
                    using (var ubisoftConnectInstallDirKey = ubisoftConnectInstallsKey.OpenSubKey(subKeyName))
                    {
                        if (ubisoftConnectInstallDirKey is null)
                        {
                            break;
                        }

                        var gameInstallDir = ubisoftConnectInstallDirKey.GetValue("InstallDir") as string;

                        if (drives.Any(d => !d.IsEnabled && gameInstallDir.ToLower().StartsWith(d.DriveLetter.ToLower())))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(gameInstallDir) == false)
                        {
                            installedTitles[installId] = new UbisoftGameRegistryRecord()
                            {
                                InstallId = installId,
                                InstallPath = PathHelpers.NormalizePath(gameInstallDir),
                            };
                        }
                    }
                }
            }
        }

        return installedTitles;
    }
}
