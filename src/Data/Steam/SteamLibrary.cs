using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.Steam
{
    internal partial class SteamLibrary : GameLibraryBase<SteamGame>, IGameLibrary
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

        public async Task LoadGamesFromCacheAsync(IEnumerable<LogicalDriveState> drives) => await base.LoadGamesFromCacheAsync(drives);

        public async Task<List<Game>> ListGamesAsync(IEnumerable<LogicalDriveState> drives, bool forceNeedsProcessing = false)
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
                    var libraryFoldersFileText = await File.ReadAllTextAsync(libraryFoldersFile).ConfigureAwait(false);

                    var matches = LibraryFoldersRegex().Matches(libraryFoldersFileText);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            // This is weird, but for some reason some libraryfolders.vdf are formatted very differently than others.
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

            foreach (string? libraryFolder in libraryFolders)
            {
                if (Directory.Exists(libraryFolder))
                {
                    string[] appManifestPaths = Directory.GetFiles(libraryFolder, "appmanifest_*.acf");
                    foreach (var appManifestPath in appManifestPaths)
                    {
                        // Don't bother adding Steamworks Common Redistributables.
                        if (appManifestPath.EndsWith("appmanifest_228980.acf") == true)
                        {
                            continue;
                        }

                        try
                        {
                            SteamGame? extractedGame = await ExtractGame(drives, appManifestPath);

                            if (extractedGame is null)
                                continue;

                            await extractedGame.SaveToDatabaseAsync().ConfigureAwait(false);

                            if (extractedGame.NeedsProcessing || forceNeedsProcessing)
                            {
                                extractedGame.ProcessGame();
                            }

                            games.Add(extractedGame);
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err);
                            continue;
                        }
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

        private async Task<SteamGame?> ExtractGame(IEnumerable<LogicalDriveState> drives, string appManifestPath)
        {
            string appManifest = await File.ReadAllTextAsync(appManifestPath).ConfigureAwait(false);

            MatchCollection? matches = AppIdRegex().Matches(appManifest);
            if (matches.Count == 0)
            {
                return null;
            }

            string steamGameAppId = matches[0].Groups["appid"].Value;
            SteamGame? game = new SteamGame(steamGameAppId);

            Match? stateFlagsMatch = StateFlagsRegex().Match(appManifest);
            if (!stateFlagsMatch.Success || !Enum.TryParse(stateFlagsMatch.Groups["StateFlags"].Value, out SteamStateFlag stateFlags))
            {
                // The AppState couldn't be parsed from the appmanifest_*.acf
                return null;
            }

            game.StateFlags = stateFlags;

            matches = NameRegex().Matches(appManifest);
            if (matches.Count == 0)
            {
                return null;
            }

            game.Title = matches[0].Groups["name"].ToString();

            matches = InstallDirRegex().Matches(appManifest);
            if (matches.Count == 0)
            {
                return null;
            }


            string? baseDir = Path.GetDirectoryName(appManifestPath);
            if (drives.Any(d => !d.IsEnabled && baseDir.ToLower().StartsWith(d.DriveLetter.ToLower())))
            {
                return null;
            }

            if (string.IsNullOrEmpty(baseDir))
            {
                return null;
            }

            string installDir = matches[0].Groups["installdir"].ToString();
            game.InstallPath = PathHelpers.NormalizePath(Path.Combine(baseDir, "common", installDir));


            SteamGame? cachedGame = GameManager.Instance.GetGame<SteamGame>(game.PlatformId);
            SteamGame? activeGame = cachedGame ?? game;
            activeGame.Title = game.Title;  // TODO: Will this be a problem if the game is already loaded
            activeGame.InstallPath = game.InstallPath;
            activeGame.StateFlags = game.StateFlags;
            activeGame.NeedsProcessing = cachedGame is null;
            return activeGame;
        }
    }
}
