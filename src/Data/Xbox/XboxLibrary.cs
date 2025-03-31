using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace DLSS_Swapper.Data.Xbox
{
    internal class XboxLibrary : GameLibraryBase<XboxGame>, IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.XboxApp;
        public string Name => "Xbox App";

        public Type GameType => typeof(XboxGame);

        static XboxLibrary? instance = null;
        public static XboxLibrary Instance => instance ??= new XboxLibrary();

        private XboxLibrary()
        {

        }

        public bool IsInstalled()
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent()?.User?.Value ?? string.Empty, "Microsoft.GamingApp_8wekyb3d8bbwe");
            return packages.Any();
        }

        public async Task LoadGamesFromCacheAsync(IEnumerable<LogicalDriveState> drives) => await base.LoadGamesFromCacheAsync(drives);

        public async Task<List<Game>> ListGamesAsync(IEnumerable<LogicalDriveState> logicalDrives, bool forceNeedsProcessing = false)
        {
            var games = new List<Game>();

            if (IsInstalled() == false)
            {
                return games;
            }

            var cachedGames = GameManager.Instance.GetGames<XboxGame>();

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

                                    var gameNode = xmlDocument.DocumentElement?.SelectSingleNode("/Game");
                                    if (gameNode is null)
                                    {
                                        continue;
                                    }
                                    var configVersion = gameNode.Attributes?["configVersion"];
                                    if (configVersion is null)
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
                                    if (identityNode is null)
                                    {
                                        continue;
                                    }

                                    var identityNodeName = identityNode.Attributes?["Name"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(identityNodeName) == true)
                                    {
                                        continue;
                                    }

                                    var shellVisualsNode = gameNode.SelectSingleNode("ShellVisuals");
                                    if (shellVisualsNode?.Attributes is null)
                                    {
                                        continue;
                                    }

                                    var potentialIcons = new List<string>();

                                    // These are added in order as these are the way we wish to use them.
                                    var splashScreenImage = shellVisualsNode.Attributes?["SplashScreenImage"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(splashScreenImage) == false)
                                    {
                                        potentialIcons.Add(splashScreenImage);
                                    }

                                    var square480x480Logo = shellVisualsNode.Attributes?["Square480x480Logo"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(square480x480Logo) == false)
                                    {
                                        potentialIcons.Add(square480x480Logo);
                                    }

                                    var square150x150Logo = shellVisualsNode.Attributes?["Square150x150Logo"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(square150x150Logo) == false)
                                    {
                                        potentialIcons.Add(square150x150Logo);
                                    }

                                    var storeLogo = shellVisualsNode.Attributes?["StoreLogo"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(storeLogo) == false)
                                    {
                                        potentialIcons.Add(storeLogo);
                                    }

                                    var square44x44Logo = shellVisualsNode.Attributes?["Square44x44Logo"]?.Value ?? string.Empty;
                                    if (string.IsNullOrEmpty(square44x44Logo) == false)
                                    {
                                        potentialIcons.Add(square44x44Logo);
                                    }

                                    gameNamesToFindPackages[identityNodeName] = potentialIcons;
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
            var packages = packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User?.Value ?? string.Empty);
            foreach (var package in packages)
            {
                if (package is null)
                {
                    continue;
                }

                string? packageName = package.Id?.Name ?? string.Empty;

                if (gameNamesToFindPackages.ContainsKey(packageName))
                {
                    try
                    {
                        XboxGame? extractedGame = ExtractGame(logicalDrives, package);

                        if (extractedGame is null)
                        {
                            continue;
                        }

                        await extractedGame.SetLocalHeaderImagesAsync(gameNamesToFindPackages[packageName]);
                        //await game.UpdateCacheImageAsync();
                        await extractedGame.SaveToDatabaseAsync();

                        // If the game is not from cache, force processing


                        if (extractedGame.NeedsProcessing || forceNeedsProcessing)
                        {
                            extractedGame.ProcessGame();
                        }
                        games.Add(extractedGame);
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err);
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

        private XboxGame? ExtractGame(IEnumerable<LogicalDriveState> drives, Package? package)
        {
            if (Directory.Exists(package.InstalledPath) == false)
            {
                return null;
            }

            if (drives.Any(d => !d.IsEnabled && package.InstalledPath.ToLower().StartsWith(d.DriveLetter.ToLower())))
            {
                return null;
            }

            var familyName = package.Id?.FamilyName ?? string.Empty;
            if (string.IsNullOrEmpty(familyName))
            {
                return null;
            }


            var cachedGame = GameManager.Instance.GetGame<XboxGame>(familyName);
            var activeGame = cachedGame ?? new XboxGame(familyName);
            activeGame.Title = package.DisplayName;  // TODO: Will this be a problem if the game is already loaded
            activeGame.InstallPath = PathHelpers.NormalizePath(package.InstalledPath);
            activeGame.NeedsProcessing = cachedGame is null;
            return activeGame;
        }
    }
}
