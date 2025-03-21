using System;

namespace DLSS_Swapper.Data.Steam;

[Flags]
internal enum SteamAppState: uint
{
    StateInvalid = 0,                  // 0
    StateUninstalled = 1 << 0,         // 1
    StateUpdateRequired = 1 << 1,      // 2
    StateFullyInstalled = 1 << 2,      // 4
    StateEncrypted = 1 << 3,           // 8
    StateLocked = 1 << 4,              // 16
    StateFilesMissing = 1 << 5,        // 32
    StateAppRunning = 1 << 6,          // 64
    StateFilesCorrupt = 1 << 7,        // 128
    StateUpdateRunning = 1 << 8,       // 256
    StateUpdatePaused = 1 << 9,        // 512
    StateUpdateStarted = 1 << 10,      // 1024
    StateUninstalling = 1 << 11,       // 2048
    StateBackupRunning = 1 << 12,      // 4096
    StateReconfiguring = 1 << 16,      // 65536
    StateValidating = 1 << 17,         // 131072
    StateAddingFiles = 1 << 18,        // 262144
    StatePreallocating = 1 << 19,      // 524288
    StateDownloading = 1 << 20,        // 1048576
    StateStaging = 1 << 21,            // 2097152
    StateCommitting = 1 << 22,         // 4194304
    StateUpdateStopping = 1 << 23      // 8388608
}

internal static class SteamAppStateExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="SteamAppState"/> means the game is ready to play.
    /// </summary>
    /// <param name="state">The state to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified state means the game is ready to play; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsReadyToPlay(this SteamAppState state)
    {
        const SteamAppState allowedFlags = SteamAppState.StateFullyInstalled | SteamAppState.StateAppRunning;
        return state != 0 && (state & ~allowedFlags) == 0;
    }
}

