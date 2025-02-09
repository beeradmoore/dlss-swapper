using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Helpers;

internal class SystemDetails
{
    public string GetSystemData()
    {
        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("```");

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


            stringBuilder.AppendLine("```");

            return stringBuilder.ToString();
        }
        catch (Exception err)
        {
            return $"Error: {err.Message}";
        }
    }
}
