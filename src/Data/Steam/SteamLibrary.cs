using DLSS_Swapper.Data.Steam.Manifest;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ValveKeyValue;

namespace DLSS_Swapper.Data.Steam;

internal partial class SteamLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.Steam;
    public string Name => "Steam";

    public Type GameType => typeof(SteamGame);

    static SteamLibrary? instance;
    public static SteamLibrary Instance => instance ??= new SteamLibrary();

    GameLibrarySettings? _gameLibrarySettings;
    public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

    static string _installPath = string.Empty;

    private SteamLibrary()
    {

    }

    public bool IsInstalled()
    {
        return string.IsNullOrEmpty(GetInstallPath()) == false;
    }

    readonly string[] _defaultHiddenGames = [
        "228980", // Steamworks Common Redistributables
];

    public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing = false)
    {
        // If we don't detect a steam install patg return an empty list.
        if (IsInstalled() == false)
        {
            return new List<Game>();
        }

        var cachedGames = GameManager.Instance.GetGames<SteamGame>();

        var installPath = GetInstallPath();


        // I hope this runs on a background thread. 
        // Tasks are whack.

        // Base steamapps folder contains libraryfolders.vdf which has references to other steamapps folders and individual installed Steam games.
        // All of these folders contain appmanifest_[some_id].acf which contains information about the game.

        var baseSteamAppsFolder = Path.Combine(installPath, "steamapps");
        var libraryFoldersFile = Path.Combine(baseSteamAppsFolder, "libraryfolders.vdf");
        if (File.Exists(libraryFoldersFile) == false)
        {
            return new List<Game>();
        }

        var allAppManifestPaths = new List<string>();
        var kvSerializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        try
        {
            using (var fileStream = File.OpenRead(libraryFoldersFile))
            {
                var libraryFoldersVDF = kvSerializer.Deserialize<Dictionary<string, LibraryFoldersVDF>>(fileStream);
                foreach (var libraryFolderVDF in libraryFoldersVDF)
                {
                    var path = PathHelpers.NormalizePath(libraryFolderVDF.Value.Path);
                    path = Path.Combine(path, "steamapps");

                    foreach (var steamApp in libraryFolderVDF.Value.Apps)
                    {
                        // Here steamApp.Value would be size on disk.
                        var appManifestPath = Path.Combine(path, $"appmanifest_{steamApp.Key}.acf");
                        if (File.Exists(appManifestPath))
                        {
                            allAppManifestPaths.Add(appManifestPath);
                        }
                        else
                        {
                            Logger.Error($"Expected manifest path was not found - {appManifestPath}");
                        }
                    }
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, $"Unable to process {libraryFoldersFile}");
            Debugger.Break();
        }

        var games = new List<Game>();

        foreach (var appManifestPath in allAppManifestPaths)
        {
            SteamGame? game;

            try
            {
                using (var fileStream = File.OpenRead(appManifestPath))
                {
                    var appManifestACF = kvSerializer.Deserialize<AppManifestACF>(fileStream);

                    if (appManifestACF is null || string.IsNullOrEmpty(appManifestACF.AppId))
                    {
                        Logger.Error($"Unable to parse app manifest - {appManifestPath}");
                        continue;
                    }

                    game = new SteamGame(appManifestACF.AppId);

                    if (Enum.TryParse(appManifestACF.StateFlags, out SteamStateFlag stateFlags) == false)
                    {
                        // The AppState couldn't be parsed from the appmanifest_*.acf
                        Logger.Error($"Unable to parse StateFlags {appManifestACF.StateFlags} for app {appManifestACF.AppId} in {appManifestPath}");
                        continue;
                    }
                    game.StateFlags = stateFlags;
                    game.Title = appManifestACF.Name;

                    var baseDir = Path.GetDirectoryName(appManifestPath);
                    if (string.IsNullOrEmpty(baseDir))
                    {
                        continue;
                    }

                    var installDir = PathHelpers.NormalizePath(Path.Combine(baseDir, "common", appManifestACF.InstallDir));
                    if (Directory.Exists(installDir) == false)
                    {
                        // If the install directory does not exist, skip this game.
                        Logger.Error($"SteamLibary could not load game {game.Title} ({game.PlatformId}) because install path does not exist: {installDir}");
                        continue;
                    }
                    game.InstallPath = installDir;
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
                continue;
            }

            var cachedGame = GameManager.Instance.GetGame<SteamGame>(game.PlatformId);
            var activeGame = cachedGame ?? game;

            if (activeGame.IsHidden is null && _defaultHiddenGames.Contains(activeGame.PlatformId))
            {
                activeGame.IsHidden = true;
            }


            activeGame.Title = game.Title;  // TODO: Will this be a problem if the game is already loaded
            activeGame.InstallPath = game.InstallPath;
            activeGame.StateFlags = game.StateFlags;

            if (activeGame.IsInIgnoredPath())
            {
                continue;
            }

            await activeGame.SaveToDatabaseAsync().ConfigureAwait(false);

            // If the game is not from cache, force processing
            if (cachedGame is null)
            {
                activeGame.NeedsProcessing = true;
            }

            if (activeGame.NeedsProcessing == true || forceNeedsProcessing == true)
            {
                activeGame.ProcessGame();
            }

            games.Add(activeGame);
        }


        games.Sort();

        // Delete games that are no longer loaded, they are likely uninstalled
        foreach (var cachedGame in cachedGames)
        {
            // Game is to be deleted.
            if (games.Contains(cachedGame) == false)
            {
                await cachedGame.DeleteAsync().ConfigureAwait(false);
            }
        }

        return games;
    }

    public static string GetInstallPath()
    {
        if (string.IsNullOrEmpty(_installPath) == false)
        {
            return _installPath;
        }

        try
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var steamRegistryKey = hklm.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    // if steamRegistryKey is null then Steam is not installed.
                    if (steamRegistryKey is null)
                    {
                        return string.Empty;
                    }

                    var installPath = steamRegistryKey.GetValue("InstallPath") as string ?? string.Empty;
                    if (string.IsNullOrEmpty(installPath) == false && Directory.Exists(installPath))
                    {
                        _installPath = installPath;
                    }

                    return _installPath;
                }
            }
        }
        catch (Exception err)
        {
            _installPath = string.Empty;
            Logger.Error(err);
            return string.Empty;
        }
    }

    public async Task LoadGamesFromCacheAsync()
    {
        try
        {
            SteamGame[] games;
            using (await Database.Instance.Mutex.LockAsync())
            {
                games = await Database.Instance.Connection.Table<SteamGame>().ToArrayAsync().ConfigureAwait(false);
            }
            foreach (var game in games)
            {
                if (game.IsInIgnoredPath())
                {
                    continue;
                }

                if (Directory.Exists(game.InstallPath) == false)
                {
                    Logger.Warning($"{Name} library could not load game {game.Title} ({game.PlatformId}) from cache because install path does not exist: {game.InstallPath}");
                    // We remove the list of known game assets, but not the game itself.
                    // Removing the game will remove its history, notes, and other data.
                    // We don't want to do this in case it is just a temporary issue.
                    await game.RemoveGameAssetsFromCacheAsync().ConfigureAwait(false);
                    continue;
                }

                await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            Debugger.Break();
        }
    }
}
