using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    public class LocalDll : IComparable<LocalDll>
    {
        public string Filename { get; }

        public Version Version { get; }

        public ulong VersionNumber { get; }

        public string SHA1Hash { get; }

        public string MD5Hash { get; }

        public LocalDll(string filename)
        {
            Filename = filename;

            var versionInfo = FileVersionInfo.GetVersionInfo(filename);

            Version = new Version(versionInfo.FileVersion.Replace(',', '.'));
            VersionNumber = ((ulong)Version.Major << 48) +
                         ((ulong)Version.Minor << 32) +
                         ((ulong)Version.Build << 16) +
                         ((ulong)Version.Revision);

            using (var stream = File.OpenRead(filename))
            {
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(stream);
                    MD5Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }

                stream.Position = 0;

                using (var sha1 = SHA1.Create())
                {
                    var hash = sha1.ComputeHash(stream);
                    SHA1Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }
        }

        public override string ToString()
        {
            return Version.ToString();
        }

        public int CompareTo(LocalDll other)
        {
            if (other == null)
            {
                return -1;
            }

            return (other.VersionNumber.CompareTo(VersionNumber));
        }
    }
}
