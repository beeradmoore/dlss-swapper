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

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

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


        public async Task<List<Game>> ListGamesAsync(bool forceLoadAll = false)
        {
            _loadedGames.Clear();
            _loadedDLSSGames.Clear();

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
                        var gameFromCache = GameManager.Instance.GetGame<XboxGame>(familyName);
                        var game = gameFromCache  ?? new XboxGame(familyName);
                        game.Title = package.DisplayName;
                        game.InstallPath = PathHelpers.NormalizePath(package.InstalledPath);
                        game.SetLocalHeaderImagesAsync(gameNamesToFindPackages[packageName]);
                        //await game.UpdateCacheImageAsync();
                        await game.SaveToDatabaseAsync();

                        // If the game does not need a reload, check if we loaded from cache.
                        // If we didn't load it from cache we will later need to call ProcessGame.
                        if (game.NeedsReload == false && gameFromCache is null)
                        {
                            game.NeedsReload = true;
                        }

                        if (game.NeedsReload == true || forceLoadAll == true)
                        {
                            game.ProcessGame();
                        }
                        games.Add(game);
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err.Message);
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

            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasSwappableItems == true));

            // Dumb workaround for async Task method. 
            await Task.Delay(10);

            return games;
        }

        public async Task LoadGamesFromCacheAsync()
        {
            try
            {
                var games = await App.CurrentApp.Database.Table<XboxGame>().ToArrayAsync().ConfigureAwait(false);
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
