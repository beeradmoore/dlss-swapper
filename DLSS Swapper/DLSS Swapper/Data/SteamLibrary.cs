using DLSS_Swapper.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sledge.Formats.Valve;

namespace DLSS_Swapper.Data
{
    class SteamLibrary : IGameLibrary
    {
        public string Name => "Steam";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled()
        {
            return GetInstallPath() != null;
        }


        public async Task<List<Game>> ListGamesAsync(IProgress<Game> progress)
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            // If we don't detect a steam install path return an empty list.
            var installPath = GetInstallPath();
            if (String.IsNullOrWhiteSpace(installPath))
            {
                return new List<Game>();
            }

            // I hope this runs on a background thread. 
            // Tasks are whack.
            return await Task.Run<List<Game>>(() =>
            {
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
                        StreamReader sr = new StreamReader(libraryFoldersFile);
                        SerialisedObjectFormatter fmt = new SerialisedObjectFormatter();
                        IEnumerable<SerialisedObject> libraryFolderFile = fmt.Deserialize(sr.BaseStream);

                        foreach (var libraryFile in libraryFolderFile)
                        {
                            foreach (var library in libraryFile.Children)
                            {
                                libraryFolders.Add(Path.Combine(library.Properties.FirstOrDefault(x => x.Key == "path").Value, "steamapps"));
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
                                progress.Report(game);
                                _loadedGames.Add(game);
                            }
                        }
                    }
                }
                _loadedGames.Sort();

                return _loadedGames;
            });
        }

        string? GetInstallPath()
        {
            try
            {
                // Only focused on x64 machines.
                var steamRegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                // if steamRegistryKey is null then steam is not installed.
                var installPath = steamRegistryKey?.GetValue("InstallPath") as String;
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
                var game = new Game();

                StreamReader sr = new StreamReader(appManifestPath);
                SerialisedObjectFormatter fmt = new SerialisedObjectFormatter();
                SerialisedObject appManifestFile = fmt.Deserialize(sr.BaseStream).FirstOrDefault();

                game.Title = appManifestFile.Properties.FirstOrDefault(x => x.Key == "name").Value;

                string installDir = appManifestFile.Properties.FirstOrDefault(x => x.Key == "installdir").Value;
                string baseDir = Path.GetDirectoryName(appManifestPath);
                game.InstallPath = Path.Combine(baseDir, "common", installDir);

                game.HeaderImage = $"https://steamcdn-a.akamaihd.net/steam/apps/{appManifestFile.Properties.FirstOrDefault(x => x.Key == "appid").Value}/library_600x900_2x.jpg"; // header.jpg";
                
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
