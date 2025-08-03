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
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Windows.Management.Deployment;
using Windows.UI.StartScreen;
using Windows.UI.Text.Core;

namespace DLSS_Swapper.Data.Xbox
{
    internal class XboxLibrary : IGameLibrary
    {
        public GameLibrary GameLibrary => GameLibrary.XboxApp;
        public string Name => "Xbox App";

        public Type GameType => typeof(XboxGame);

        static XboxLibrary? instance = null;
        public static XboxLibrary Instance => instance ??= new XboxLibrary();

        GameLibrarySettings? _gameLibrarySettings;
        public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

        private XboxLibrary()
        {

        }

        public bool IsInstalled()
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent()?.User?.Value ?? string.Empty, "Microsoft.GamingApp_8wekyb3d8bbwe");
            return packages.Any();
        }

        readonly string[] _defaultHiddenGames = [
            "38985CA0.ChicagoDLC04StandardPack01_5bkah9njm3e9g", // Tony Hawk's™ Pro Skater™ 3 + 4 (DLC)
            "38985CA0.ChicagoDLC02DigitalDeluxePack01_5bkah9njm3e9g", // Tony Hawk's™ Pro Skater™ 3 + 4 - Digital Deluxe Edition (DLC)
            "38985CA0.ChicagoDLC03POPack01_5bkah9njm3e9g", // Tony Hawk's™ Pro Skater™ 3 + 4 - Pre Order Pack (DLC)
        ];

        public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing = false)
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

                var packageName = package.Id?.Name ?? string.Empty;

                if (gameNamesToFindPackages.ContainsKey(packageName))
                {
                    if (Directory.Exists(package.InstalledPath) == false)
                    {
                        continue;
                    }

                    var familyName = package.Id?.FamilyName ?? string.Empty;
                    if (string.IsNullOrEmpty(familyName))
                    {
                        continue;
                    }

                    try
                    {
                        var cachedGame = GameManager.Instance.GetGame<XboxGame>(familyName);
                        var activeGame = cachedGame ?? new XboxGame(familyName);

                        if (activeGame.IsHidden is null && _defaultHiddenGames.Contains(activeGame.PlatformId))
                        {
                            activeGame.IsHidden = true;
                        }

                        activeGame.Title = package.DisplayName;  // TODO: Will this be a problem if the game is already loaded
                        activeGame.InstallPath = PathHelpers.NormalizePath(package.InstalledPath);

                        if (activeGame.IsInIgnoredPath())
                        {
                            continue;
                        }

                        if (Directory.Exists(activeGame.InstallPath) == false)
                        {
                            Logger.Warning($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
                            continue;
                        }

                        await activeGame.SetLocalHeaderImagesAsync(gameNamesToFindPackages[packageName]);
                        //await game.UpdateCacheImageAsync();
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

        public async Task LoadGamesFromCacheAsync()
        {
            try
            {
                XboxGame[] games;
                using (await Database.Instance.Mutex.LockAsync())
                {
                    games = await Database.Instance.Connection.Table<XboxGame>().ToArrayAsync().ConfigureAwait(false);
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
}
