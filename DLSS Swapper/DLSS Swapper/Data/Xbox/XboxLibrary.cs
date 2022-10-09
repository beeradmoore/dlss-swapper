using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using DLSS_Swapper.Interfaces;
using Windows.Management.Deployment;
using Windows.UI.StartScreen;
using Windows.UI.Text.Core;

namespace DLSS_Swapper.Data.Xbox
{
    internal class XboxLibrary : IGameLibrary
    {
        public string Name => "Xbox App";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }



        public bool IsInstalled()
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value, "Microsoft.GamingApp_8wekyb3d8bbwe");
            return packages.Any();
        }


        public async Task<List<Game>> ListGamesAsync()
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

            var games = new List<Game>();

            if (IsInstalled() == false)
            {
                return games;
            }


            var gameNamesToFindPackages = new Dictionary<string, List<string>>();

            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                // Skip network drives and CDRom drives.
                if (drive.DriveType == DriveType.Unknown || drive.DriveType == DriveType.Network || drive.DriveType == DriveType.CDRom)
                {
                    continue;
                }

                var gamingRootFile = Path.Combine(drive.RootDirectory.FullName, ".GamingRoot");
                if (File.Exists(gamingRootFile))
                {
                    var fileBytes = File.ReadAllBytes(gamingRootFile);
                    var mystring = Encoding.Unicode.GetString(fileBytes);
                    // Validate file header.
                    //RGBX
                    if (fileBytes.Length > 5 && fileBytes[0] == 'R' && fileBytes[1] == 'G' && fileBytes[2] == 'B' && fileBytes[3] == 'X')
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append(drive.RootDirectory.FullName);
                        for (var i = 5; i < fileBytes.Length; i++)
                        {
                            // Ignore bytes that are 0
                            if (fileBytes[i] != 0)
                            {
                                stringBuilder.Append((char)fileBytes[i]);
                            }
                        }

                        var targetDir = stringBuilder.ToString();
                        if (Directory.Exists(targetDir))
                        {
                            var gameDirectories = Directory.GetDirectories(targetDir);
                            foreach (var gameDirectory in gameDirectories)
                            {
                                var configFile = Path.Combine(gameDirectory, "Content", "MicrosoftGame.config");
                                if (File.Exists(configFile))
                                {   
                                    var xmlDocument = new XmlDocument();
                                    xmlDocument.Load(configFile);

                                    var gameNode = xmlDocument.DocumentElement.SelectSingleNode("/Game");
                                    if (gameNode == null)
                                    {
                                        continue;
                                    }
                                    var configVersion = gameNode.Attributes["configVersion"];
                                    if (configVersion == null)
                                    {
                                        continue;
                                    }

                                    // Skip if we potentially don't know about this config version.
                                    if (configVersion.Value != "0" && configVersion.Value != "1")
                                    {
                                        Logger.Error($"Unknown configVersion in {configFile}, {configVersion.Value}");
                                        continue;
                                    }


                                    var identityNode = gameNode.SelectSingleNode("Identity");
                                    if (identityNode == null)
                                    {
                                        continue;
                                    }

                                    var identityNodeName = identityNode.Attributes["Name"];
                                    if (identityNodeName == null)
                                    {
                                        continue;
                                    }

                                    var shellVisualsNode = gameNode.SelectSingleNode("ShellVisuals");
                                    if (shellVisualsNode == null)
                                    {
                                        continue;
                                    }

                                    var potentialIcons = new List<string>();

                                    // These are added in order as these are the way we wish to use them.
                                    if (shellVisualsNode.Attributes["SplashScreenImage"] != null)
                                    {
                                        potentialIcons.Add(shellVisualsNode.Attributes["SplashScreenImage"].Value);
                                    }

                                    if (shellVisualsNode.Attributes["Square480x480Logo"] != null)
                                    {
                                        potentialIcons.Add(shellVisualsNode.Attributes["Square480x480Logo"].Value);
                                    }

                                    if (shellVisualsNode.Attributes["Square150x150Logo"] != null)
                                    {
                                        potentialIcons.Add(shellVisualsNode.Attributes["Square150x150Logo"].Value);
                                    }

                                    if (shellVisualsNode.Attributes["StoreLogo"] != null)
                                    {
                                        potentialIcons.Add(shellVisualsNode.Attributes["StoreLogo"].Value);
                                    }

                                    if (shellVisualsNode.Attributes["Square44x44Logo"] != null)
                                    {
                                        potentialIcons.Add(shellVisualsNode.Attributes["Square44x44Logo"].Value);
                                    }

                                    gameNamesToFindPackages[identityNodeName.Value] = potentialIcons;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"Unable to parse data of .GamingRoot file - {BitConverter.ToString(fileBytes)}");
                    }
                }
            }

            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);
            foreach (var package in packages)
            {
                var packageName = package.Id?.Name ?? String.Empty;

                if (gameNamesToFindPackages.ContainsKey(packageName))
                {
                    if (Directory.Exists(package.InstalledPath) == false)
                    {
                        continue;
                    }

                    try
                    {

                        var game = new XboxGame(gameNamesToFindPackages[packageName])
                        {
                            Title = package.DisplayName,
                            InstallPath = package.InstalledPath,
                        };
                        game.DetectDLSS();
                        games.Add(game);
                    }
                    catch (Exception err)
                    {

                    }
                }
            }

            games.Sort();
            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

            // Dumb workaround for async Task method. 
            await Task.Delay(10);

            return games;
        }
    }
}
