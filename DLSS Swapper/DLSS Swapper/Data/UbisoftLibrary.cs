using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    class UbisoftLibrary : IGameLibrary
    {
        private string _installPath;

        public bool IsInstalled() => !String.IsNullOrEmpty(_installPath);

        public UbisoftLibrary()
        {
            _installPath = GetInstallPath();
        }

        public async Task<List<Game>> ListGamesAsync()
        {
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

                            games.Add(GetGame(gamePath));
                        }
                    }

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
                game.DetectDLSS();

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
