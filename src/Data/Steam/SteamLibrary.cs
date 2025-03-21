using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.Steam
{
    internal partial class SteamLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.Steam;
        public string Name => "Steam";

        public Type GameType => typeof(SteamGame);

        static SteamLibrary? instance = null;
        public static SteamLibrary Instance => instance ??= new SteamLibrary();

        static string _installPath = string.Empty;

        [GeneratedRegex(@"^([ \t]*)""(.*)""([ \t]*)""(?<path>.*)""$", RegexOptions.Multiline)]
        private static partial Regex LibraryFoldersRegex();

        [GeneratedRegex(@"^([ \t]*)""StateFlags""([ \t]*)""(?<StateFlags>\d+)""([ \t]*)$", RegexOptions.Multiline)]
        private static partial Regex StateFlagsRegex();

        [GeneratedRegex(@"^([ \t]*)""appid""([ \t]*)""(?<appid>.*)""$", RegexOptions.Multiline)]
        private static partial Regex AppIdRegex();

        [GeneratedRegex(@"^([ \t]*)""name""([ \t]*)""(?<name>.*)""$", RegexOptions.Multiline)]
        private static partial Regex NameRegex();

        [GeneratedRegex(@"^([ \t]*)""installdir""([ \t]*)""(?<installdir>.*)""$", RegexOptions.Multiline)]
        private static partial Regex InstallDirRegex();

        private SteamLibrary()
        {

        }

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetInstallPath()) == false;
        }

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

            var games = new List<Game>();

            // Base steamapps folder contains libraryfolders.vdf which has references to other steamapps folders.
            // All of these folders contain appmanifest_[some_id].acf which contains information about the game.
            // We parse all of these files with jank regex, rather than building a parser.
            // If we ever need to this page probably contains info on how to do that, https://developer.valvesoftware.com/wiki/KeyValues

            var baseSteamAppsFolder = Path.Combine(installPath, "steamapps");

            var libraryFolders = new List<string>();
            libraryFolders.Add(Helpers.PathHelpers.NormalizePath(baseSteamAppsFolder));

            var libraryFoldersFile = Path.Combine(baseSteamAppsFolder, "libraryfolders.vdf");
            if (File.Exists(libraryFoldersFile))
            {
                try
                {
                    var libraryFoldersFileText = await File.ReadAllTextAsync(libraryFoldersFile);

                    var matches = LibraryFoldersRegex().Matches(libraryFoldersFileText);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            // This is weird, but for some reason some libraryfolders.vdf are formatted very differnetly than others.
                            var path = match.Groups["path"].ToString();
                            if (Directory.Exists(path))
                            {
                                libraryFolders.Add(Helpers.PathHelpers.NormalizePath(Path.Combine(path, "steamapps")));
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    // TODO: Report
                    Logger.Error(err, $"Unable to parse libraryfolders.vdf");
                }
            }

            // Makes sure all library folders are unique.
            libraryFolders = libraryFolders.Distinct().ToList();

            foreach (var libraryFolder in libraryFolders)
            {
                if (Directory.Exists(libraryFolder))
                {
                    var appManifestPaths = Directory.GetFiles(libraryFolder, "appmanifest_*.acf");
                    foreach (var appManifestPath in appManifestPaths)
                    {
                        // Don't bother adding Steamworks Common Redistributables.
                        if (appManifestPath.EndsWith("appmanifest_228980.acf") == true)
                        {
                            continue;
                        }

                        SteamGame? game = null;

                        try
                        {
                            var appManifest = await File.ReadAllTextAsync(appManifestPath);

                            var matches = AppIdRegex().Matches(appManifest);
                            if (matches.Count == 0)
                            {
                                continue;
                            }

                            var steamGameAppId = matches[0].Groups["appid"].Value;
                            game = new SteamGame(steamGameAppId);

                            var stateFlagsMatch = StateFlagsRegex().Match(appManifest);
                            if (!stateFlagsMatch.Success || !Enum.TryParse(stateFlagsMatch.Groups["StateFlags"].Value, out SteamAppState stateFlags))
                            {
                                // The AppState couldn't be parsed from the appmanifest_*.acf
                                continue;
                            }

                            game.AppState = stateFlags;

                            matches = NameRegex().Matches(appManifest);
                            if (matches.Count == 0)
                            {
                                continue;
                            }

                            game.Title = matches[0].Groups["name"].ToString();

                            matches = InstallDirRegex().Matches(appManifest);
                            if (matches.Count == 0)
                            {
                                continue;
                            }

                            var installDir = matches[0].Groups["installdir"].ToString();

                            var baseDir = Path.GetDirectoryName(appManifestPath);
                            if (string.IsNullOrEmpty(baseDir))
                            {
                                continue;
                            }

                            game.InstallPath = PathHelpers.NormalizePath(Path.Combine(baseDir, "common", installDir));
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err);
                            continue;
                        }

                        var cachedGame = GameManager.Instance.GetGame<SteamGame>(game.PlatformId);
                        var activeGame = cachedGame ?? game;
                        activeGame.Title = game.Title;  // TODO: Will this be a problem if the game is already loaded
                        activeGame.InstallPath = game.InstallPath;
                        activeGame.AppState = game.AppState;

                        await activeGame.SaveToDatabaseAsync();

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
                }
            }
            games.Sort();

            // Delete games that are no longer loaded, they are likely uninstalled
            foreach (var cachedGame in cachedGames)
            {
                // Game is to be deleted.
                if (games.Contains(cachedGame) == false)
                {
                    await cachedGame.DeleteAsync();
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
}
