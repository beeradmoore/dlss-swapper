using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    class UbisoftLibrary : IGameLibrary
    {
        private string _installPath;

        public string Name => "Ubisoft";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        public bool IsInstalled() => !String.IsNullOrEmpty(_installPath);

        public UbisoftLibrary()
        {
            _installPath = GetInstallPath();
        }

        public async Task<List<Game>> ListGamesAsync(IProgress<Game> progress)
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();
            var games = new List<Game>();

            return await Task.Run<List<Game>>(() =>
            {
                // Only proceed if Ubisoft Connect is installed
                if (IsInstalled())
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs"))
                    {
                        foreach (var subkey in key.GetSubKeyNames())
                        {
                            var gamekey = key.OpenSubKey(subkey)?.GetValue("InstallDir");
                            var gamePath = gamekey.ToString();

                            progress.Report(GetGame(gamePath));
                            games.Add(GetGame(gamePath));
                        }
                    }

                    games.Sort();
                    _loadedGames.AddRange(games);
                    _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

                    return games;
                }

                // Ubisoft Connect not installed
                return new List<Game>();
            });
        }


        string GetInstallPath()
        {
            try
            {
                // Only focused on x64 machines.
                var ubiRegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher");

                // if key is null then Ubisoft Connect is not installed.
                var installPath = ubiRegistryKey?.GetValue("InstallDir") as string;

                return installPath;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"IsInstalled Error: {err.Message}");
                return null;
            }
        }

        private Game GetGame(string installDir)
        {
            try
            {
                // Game name obtained from path. 
                DirectoryInfo di = new DirectoryInfo(installDir);

                Game game = new Game();
                game.Title = di.Name;
                game.InstallPath = installDir;

                return game;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Getting Game: {ex.Message}");
                return null;
            }
        }
    }
}
