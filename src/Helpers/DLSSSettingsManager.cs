using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

namespace DLSS_Swapper.Helpers;

// Reg values defined in this file are from:
// https://github.com/NVIDIA/DLSS/tree/main/utils

internal class DLSSSettingsManager
{
    const string NGXCORE_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NGXCore";

    public DLSSSettingsManager()
    {

    }

    bool RunRegAdd(string key, string name, string type, string value)
    {
        var processInfo = new ProcessStartInfo()
        {
            FileName = "reg",
            Arguments = $"add \"{key}\" /f /v {name} /t {type} /d {value}",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(processInfo))
            {
                if (process is not null)
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return true;
                    }

                    throw new Exception("Process exit code was {process.ExitCode}");
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, $"Could not command \"{processInfo.FileName} {processInfo.Arguments}");
        }

        return false;
    }

    public bool SetShowDlssIndicator(int value)
    {
        return RunRegAdd(NGXCORE_REG_KEY, "ShowDlssIndicator", "REG_DWORD", value.ToString(CultureInfo.InvariantCulture));
    }

    public int GetShowDlssIndicator()
    {
        if (Registry.GetValue(NGXCORE_REG_KEY, "ShowDlssIndicator", 0) is int existingValue)
        {
            return existingValue;
        }

        return 0;
    }


    public bool SetLogLevel(int logLevel)
    {
        if (logLevel == 0 || logLevel == 1 || logLevel == 2)
        {
            return RunRegAdd(NGXCORE_REG_KEY, "LogLevel", "REG_DWORD", $"{logLevel}");
        }

        return false;
    }

    public int GetLogLevel()
    {
        if (Registry.GetValue(NGXCORE_REG_KEY, "LogLevel", 0) is int existingValue)
        {
            return existingValue;
        }

        return 0;
    }


    public bool SetLoggingWindow(bool enabled)
    {
        if (enabled)
        {
            return RunRegAdd(NGXCORE_REG_KEY, "EnableConsoleLogging", "REG_DWORD", "1");
        }
        else
        {
            return RunRegAdd(NGXCORE_REG_KEY, "EnableConsoleLogging", "REG_DWORD", "0");
        }
    }

    public bool GetLoggingWindow()
    {
        if (Registry.GetValue(NGXCORE_REG_KEY, "EnableConsoleLogging", 0) is int existingValue)
        {
            return (existingValue == 1);
        }

        return false;
    }


}
