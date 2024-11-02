using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Extensions
{
    internal static class FileVersionInfoExtensions
    {
        internal static string GetMD5Hash(this FileVersionInfo fileVersionInfo)
        {
            try
            {
                using (var fileStream = File.OpenRead(fileVersionInfo.FileName))
                {
                    return fileStream.GetMD5Hash();
                }
            }
            catch (Exception err)
            {
                Logger.Error($"{fileVersionInfo.FileName}, {err.Message}");
                Debugger.Break();
            }

            return string.Empty;
        }

        internal static string GetFormattedFileVersion(this FileVersionInfo fileVersionInfo)
        {
            return $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}.{fileVersionInfo.FilePrivatePart}";
        }

        internal static ulong GetFileVersionNumber(this FileVersionInfo fileVersionInfo)
        {
            return ((ulong)fileVersionInfo.FileMajorPart << 48) +
                    ((ulong)fileVersionInfo.FileMinorPart << 32) +
                    ((ulong)fileVersionInfo.FileBuildPart << 16) +
                    ((ulong)fileVersionInfo.FilePrivatePart);
        }
    }
}
