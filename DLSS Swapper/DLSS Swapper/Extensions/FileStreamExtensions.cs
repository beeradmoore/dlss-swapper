using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Extensions
{
    internal static class FileStreamExtensions
    {
        internal static string GetMD5Hash(this FileStream fileStream)
        {
            fileStream.Position = 0;

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(fileStream);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }
    }
}
