using DLSS_Swapper.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    class SteamLibrary : IGameLibrary
    {
        public bool IsInstalled()
        {
            return (GetInstallPath() != null);
        }

        public async Task<List<Game>> ListGamesAsync()
        {
            // If we don't detect a steam install patg return an empty list.
            var installPath = GetInstallPath();
            if (String.IsNullOrWhiteSpace(installPath))
            {
                return new List<Game>();
            }

            // I hope this runs on a background thread. 
            // Tasks are whack.
            return await Task.Run<List<Game>>(() =>
            {
                var games = new List<Game>();

                // Base steamapps folder contains libraryfolders.vdf which has references to other steamapps folders.
                // All of these folders contain appmanifest_[some_id].acf which contains information about the game.
                // We parse all of these files with jank regex, rather than building a parser.
                // If we ever need to this page probably contains info on how to do that, https://developer.valvesoftware.com/wiki/KeyValues

                var baseSteamAppsFolder = Path.Combine(installPath, "steamapps");

                var libraryFolders = new List<string>();
                libraryFolders.Add(baseSteamAppsFolder);

                var libraryFoldersFile = Path.Combine(baseSteamAppsFolder, "libraryfolders.vdf");
                if (File.Exists(libraryFoldersFile))
                {
                    try
                    {
                        var libraryFoldersFileText = File.ReadAllText(libraryFoldersFile);

                        var regex = new Regex(@"^(\s*)""path""(\s*)""(?<path>.*)""$", RegexOptions.Multiline);
                        var matches = regex.Matches(libraryFoldersFileText);
                        if (matches.Count > 0)
                        {
                            foreach (Match match in matches)
                            {
                                var path = match.Groups["path"].ToString();
                                libraryFolders.Add(Path.Combine(path, "steamapps"));
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        // TODO: Report
                        System.Diagnostics.Debug.WriteLine($"ERROR: Unable to parse libraryfolders.vdf, {err.Message}");
                    }
                }

                foreach (var libraryFolder in libraryFolders)
                {
                    if (Directory.Exists(libraryFolder))
                    {
                        var appManifests = Directory.GetFiles(libraryFolder, "appmanifest_*.acf");
                        foreach (var appManifest in appManifests)
                        {
                            var game = GetGameFromAppManifest(appManifest);
                            if (game != null)
                            {
                                games.Add(game);
                            }
                        }
                    }
                }

                return games;
            });
        }

        string? GetInstallPath()
        {
            try
            {
                // Only focused on x64 machines.
                var steamRegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                var installPath = steamRegistryKey.GetValue("InstallPath") as String;
                return installPath;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"IsInstalled Error: {err.Message}");
                return null;
            }
        }

        internal Game GetGameFromAppManifest(string appManifestPath)
        {
            try
            {
                var appManifest = File.ReadAllText(appManifestPath);
                var game = new Game();

                var regex = new Regex(@"^(\s*)""name""(\s*)""(?<name>.*)""$", RegexOptions.Multiline);
                var matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }

                game.Title = matches[0].Groups["name"].ToString();

                regex = new Regex(@"^(\s*)""installdir""(\s*)""(?<installdir>.*)""$", RegexOptions.Multiline);
                matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }

                var installDir = matches[0].Groups["installdir"].ToString();

                var baseDir = Path.GetDirectoryName(appManifestPath);

                game.InstallPath = Path.Combine(baseDir, "common", installDir);


                regex = new Regex(@"^(\s*)""appid""(\s*)""(?<appid>.*)""$", RegexOptions.Multiline);
                matches = regex.Matches(appManifest);
                if (matches.Count == 0)
                {
                    return null;
                }
                game.HeaderImage = $"https://steamcdn-a.akamaihd.net/steam/apps/{matches[0].Groups["appid"] }/library_600x900_2x.jpg"; // header.jpg";

                game.DetectDLSS();
                return game;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetGameFromAppManifest: {err.Message}");
                return null;
            }
        }
    }
}
