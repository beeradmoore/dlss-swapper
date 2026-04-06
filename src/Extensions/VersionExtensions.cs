using System;

namespace DLSS_Swapper.Extensions;

internal static class VersionExtensions
{
    internal static ulong GetVersionNumber(this Version version)
    {
        return ((ulong)version.Major << 48) +
                ((ulong)version.Minor << 32) +
                ((ulong)version.Build << 16) +
                ((ulong)version.Revision);
    }
}
