using System;
using System.IO;
using System.Security.Cryptography;

namespace DLSS_Swapper.Extensions;

internal static class FileStreamExtensions
{
    internal static string GetMD5Hash(this Stream fileStream)
    {
        fileStream.Position = 0;

        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }

    internal static string GetSha256Hash(this Stream fileStream)
    {
        fileStream.Position = 0;

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
