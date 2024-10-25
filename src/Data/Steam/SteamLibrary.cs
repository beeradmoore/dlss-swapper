using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.Steam
{
    class SteamLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.Steam;
        public string Name => "Steam";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public Type GameType => typeof(SteamGame);

        static SteamLibrary? instance = null;
        public static SteamLibrary Instance => instance ??= new SteamLibrary();

        static string _installPath = string.Empty;

        private SteamLibrary()
        {

        }

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetInstallPath()) == false;
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            // If we don't detect a steam install patg return an empty list.
            var installPath = GetInstallPath();
            if (string.IsNullOrEmpty(installPath))
            {
                return new List<Game>();
            }

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
                    var libraryFoldersFileText = File.ReadAllText(libraryFoldersFile);

                    var regex = new Regex(@"^([ \t]*)""(.*)""([ \t]*)""(?<path>.*)""$", RegexOptions.Multiline);
                    var matches = regex.Matches(libraryFoldersFileText);
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
                    Logger.Error($"Unable to parse libraryfolders.vdf, {err.Message}");
                }
            }

            // Makes sure all library folders are unique.
            libraryFolders = libraryFolders.Distinct().ToList();

            foreach (var libraryFolder in libraryFolders)
            {
                if (Directory.Exists(libraryFolder))
                {
                    var appManifests = Directory.GetFiles(libraryFolder, "appmanifest_*.acf");
                    foreach (var appManifest in appManifests)
                    {
                        // Don't bother adding Steamworks Common Redistributables.
                        if (appManifest.EndsWith("appmanifest_228980.acf") == true)
                        {
                            continue;
                        }

                        var game = GetGameFromAppManifest(appManifest);
                        if (game is not null)
                        {
                            await game.SaveToDatabaseAsync();
                            game.ProcessGame();
                            games.Add(game);
                        }
                    }
                }
            }
            games.Sort();
            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

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
                        // if steamRegistryKey is null then steam is not installed.
                        _installPath = (steamRegistryKey?.GetValue("InstallPath") as string) ?? string.Empty;
                        
                        return _installPath;
                    }
                }
            }
            catch (Exception err)
            {
                _installPath = string.Empty;
                Logger.Error(err.Message);
                return string.Empty;
            }
        }

        internal Game? GetGameFromAppManifest(string appManifestPath)
        {
            try
            {
                var appManifest = File.ReadAllText(appManifestPath);

                var regex = new Regex(@"^([ \t]*)""appid""([ \t]*)""(?<appid>.*)""$", RegexOptions.Multiline);
                var matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }

                var steamGameAppId = matches[0].Groups["appid"].Value;
                var game = GameManager.Instance.GetGame<SteamGame>(steamGameAppId) ?? new SteamGame(steamGameAppId);

                regex = new Regex(@"^([ \t]*)""name""([ \t]*)""(?<name>.*)""$", RegexOptions.Multiline);
                matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }

                game.Title = matches[0].Groups["name"].ToString();

                regex = new Regex(@"^([ \t]*)""installdir""([ \t]*)""(?<installdir>.*)""$", RegexOptions.Multiline);
                matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }

                var installDir = matches[0].Groups["installdir"].ToString();

                var baseDir = Path.GetDirectoryName(appManifestPath);
                if (string.IsNullOrEmpty(baseDir))
                {
                    return null;
                }

                game.InstallPath = Path.Combine(baseDir, "common", installDir);

                return game;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return null;
            }
        }

        public async Task<List<Game>> LoadFromCacheAsync()
        {
            try
            {
                var games = await App.CurrentApp.Database.Table<SteamGame>().ToListAsync();
                return games.ToList<Game>();
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
            return new List<Game>();
        }

        public async Task LoadGamesAsync()
        {
            await Task.Delay(1);
        }

        public async Task LoadGamesFromCacheAsync()
        {
            try
            {
                var games = await App.CurrentApp.Database.Table<SteamGame>().ToArrayAsync().ConfigureAwait(false);
                foreach (var game in games)
                {
                    await game.LoadGameAssetsFromCacheAsync().ConfigureAwait(false);
                    GameManager.Instance.AddGame(game);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}
