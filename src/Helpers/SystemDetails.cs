using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Data;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Helpers;

internal class SystemDetails
{
    public string GetSystemData()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("```");
        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();


            stringBuilder.AppendLine($"DLSS Swapper: {App.CurrentApp.GetVersionString()}");
#if PORTABLE
            stringBuilder.AppendLine("Portable: true");
#else
            stringBuilder.AppendLine("Portable: false");
#endif
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("System");
            try
            {
                string query = "SELECT * FROM Win32_OperatingSystem";
                var searcher = new System.Management.ManagementObjectSearcher(query);

                foreach (var os in searcher.Get())
                {
                    stringBuilder.AppendLine($"OS: {os["Caption"]}");
                }
            }
            catch (Exception)
            {
                // NOOP
            }
            stringBuilder.AppendLine($"OSVersion: {Environment.OSVersion.VersionString}");
            stringBuilder.AppendLine($"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
            stringBuilder.AppendLine($"Is64BitProcess: {Environment.Is64BitProcess}");
            stringBuilder.AppendLine($"Runtime: {Environment.Version}");


            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Permissions");
            stringBuilder.AppendLine($"IsPrivilegedProcess: {Environment.IsPrivilegedProcess}");

            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                stringBuilder.AppendLine($"IsInRole Administrator: {principal.IsInRole(WindowsBuiltInRole.Administrator)}");
                stringBuilder.AppendLine($"IsInRole User: {principal.IsInRole(WindowsBuiltInRole.User)}");
            }


            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Paths");
            stringBuilder.AppendLine($"StoragePath: {Storage.StoragePath}");
            stringBuilder.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
            stringBuilder.AppendLine($"GetCurrentDirectory: {Directory.GetCurrentDirectory()}");
            stringBuilder.AppendLine($"AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
            stringBuilder.AppendLine($"Assembly Location: {currentAssembly.Location}");
            stringBuilder.AppendLine($"ProcessPath: {Environment.ProcessPath ?? string.Empty}");

        }
        catch (Exception err)
        {
            stringBuilder.AppendLine($"ERROR: {err.Message}");
        }
        finally
        {
            stringBuilder.AppendLine("```");
        }

        return stringBuilder.ToString();
    }

    public string GetLibraryData()
    {
        var gameList = GameManager.Instance.GetSynchronisedGamesListCopy();
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("```");

        try
        {
            foreach (var gameLibraryEnum in Enum.GetValues<GameLibrary>())
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                stringBuilder.AppendLine(gameLibrary.Name);
                stringBuilder.AppendLine($"Status: {(gameLibrary.IsEnabled ? "Enabled" : "Disabled")}");
                if (gameLibrary.IsEnabled)
                {
                    stringBuilder.AppendLine($"Games: {gameList.Count(x => x.GameLibrary == gameLibrary.GameLibrary)}");
                }
                stringBuilder.AppendLine();
            }
        }
        catch (Exception err)
        {
            stringBuilder.AppendLine($"ERROR: {err.Message}");
        }
        finally
        {
            stringBuilder.AppendLine("```");
        }

        return stringBuilder.ToString();
    }
}
