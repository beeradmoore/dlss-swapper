using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Helpers.Utils;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    public class UbisoftConnectLibrary : GameLibraryBase<UbisoftConnectGame>, IGameLibrary
    {
        public record UbisoftRecord
        {
            internal int Size { get; init; } = 0;
            internal int Offset { get; init; } = 0;
            internal int InstallId { get; init; } = 0;
            internal int LaunchId { get; init; } = 0;
        }

        public record UbisoftGameRegistryRecord
        {
            internal int InstallId { get; init; } = 0;
            internal string InstallPath { get; init; } = string.Empty;
        }

        public GameLibrary GameLibrary => GameLibrary.UbisoftConnect;
        public string Name => "Ubisoft Connect";

        public Type GameType => typeof(UbisoftConnectGame);

        static UbisoftConnectLibrary? instance = null;
        public static UbisoftConnectLibrary Instance => instance ??= new UbisoftConnectLibrary();

        private UbisoftConnectLibrary()
        {

        }

        string _installPath = string.Empty;

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetInstallPath()) == false;
        }

        private UbisoftConnectGame? ExtractGame(IEnumerable<LogicalDriveState> drives, string assetsPath, Dictionary<int, UbisoftGameRegistryRecord> installedTitles, UbisoftConnectConfigurationItem ubisoftConnectConfigurationItem, UbisoftRecord ubisoftRecord)
        {
            // This is less than ideal. This usually happens with DLC or pre-orders. 
            if (ubisoftConnectConfigurationItem is null || ubisoftConnectConfigurationItem.Root is null)
            {
                Logger.Info("Could not load Ubisoft Connect item. This is sometimes expected for certain titles.");
                return null;
            }

            string installPath = installedTitles[ubisoftRecord.InstallId].InstallPath;
            if (drives.Any(d => !d.IsEnabled && installPath.ToLower().StartsWith(d.DriveLetter.ToLower())))
            {
                return null;
            }

            // Unsure if we care about version at the moment. 
            /*
            if (ubisoftConnectConfigurationItem.Version != "2.0")
            {
                // If Version isn't 2.0 we will just not load any games.
                Logger.Error($"Unknown item version. Expected 2.0, found {ubisoftConnectConfigurationItem.Version}");
                continue;
            }
            */

            // This can be expected. If there is no installer item there is no game to install.
            if (ubisoftConnectConfigurationItem.Root.Installer is null)
            {
                return null;
            }

            // This is not expected. 
            if (ubisoftConnectConfigurationItem.Root.StartGame is null)
            {
                Logger.Info($"StartGameNode is null for {ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier}. This is likely a region specific installer.");
                return null;
            }

            var localImage = string.Empty;
            var remoteImage = string.Empty;
            if (ubisoftConnectConfigurationItem.Root.LogoImage is not null)
            {
                if (ubisoftConnectConfigurationItem.Root.ThumbImage.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || ubisoftConnectConfigurationItem.Root.ThumbImage.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    localImage = Path.Combine(assetsPath, ubisoftConnectConfigurationItem.Root.ThumbImage);
                    remoteImage = $"https://ubistatic3-a.akamaihd.net/orbit/uplay_launcher_3_0/assets/{ubisoftConnectConfigurationItem.Root.ThumbImage}";
                }
                else
                {
                    // Hopefully if we need to check localizations that it is in the default key.
                    // In future if we do actual localization then we need to check persons locale and apply that here.
                    if (ubisoftConnectConfigurationItem.Localizations?.ContainsKey("default") == true)
                    {
                        if (ubisoftConnectConfigurationItem.Localizations["default"]?.ContainsKey(ubisoftConnectConfigurationItem.Root.ThumbImage) == true)
                        {
                            localImage = Path.Combine(assetsPath, ubisoftConnectConfigurationItem.Localizations["default"][ubisoftConnectConfigurationItem.Root.ThumbImage]);
                            remoteImage = $"https://ubistatic3-a.akamaihd.net/orbit/uplay_launcher_3_0/assets/{ubisoftConnectConfigurationItem.Localizations["default"][ubisoftConnectConfigurationItem.Root.ThumbImage]}";
                        }
                    }
                }
            }

            var cachedGame = GameManager.Instance.GetGame<UbisoftConnectGame>(ubisoftRecord.InstallId.ToString());
            var activeGame = cachedGame ?? new UbisoftConnectGame(ubisoftRecord.InstallId.ToString());

            activeGame.Title = ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier;  // TODO: Will this be a problem if the game is already loaded
            activeGame.InstallPath = PathHelpers.NormalizePath(installedTitles[ubisoftRecord.InstallId].InstallPath);
            activeGame.LocalHeaderImage = localImage;
            activeGame.RemoteHeaderImage = remoteImage;
            activeGame.NeedsProcessing = cachedGame is null;
            return activeGame;
        }

        public async Task LoadGamesFromCacheAsync(IEnumerable<LogicalDriveState> drives) => await base.LoadGamesFromCacheAsync(drives);

        public async Task<List<Game>> ListGamesAsync(IEnumerable<LogicalDriveState> drives, bool forceNeedsProcessing = false)
        {
            var games = new List<Game>();

            if (IsInstalled() == false)
            {
                return games;
            }

            var cachedGames = GameManager.Instance.GetGames<UbisoftConnectGame>();

            // Gat a list of installed games.
            // NOTE: Some games are installed from Ubisoft Connect via Steam (eg. Far Cry: Blood Dragon)
            // Those titles will show up in the Steam games list.
            // Ironically Ubisoft Connect may show double gamess listed here if you do indeed own it from uplay/ubisoft connect and from 3rd party stores.
            var installedTitles = new Dictionary<int, UbisoftGameRegistryRecord>();
            try
            {
                installedTitles = UbisoftConnectUtils.CreateInstalledTitlesDictionary(drives);
            }
            catch (Exception err)
            {
                Logger.Error(err, $"Error getting list of installs");
                return games;
            }

            // Could not detect any installed games.
            if (installedTitles.Count == 0)
            {
                Logger.Info("Unable to load any Ubisoft Connect games, maybe none are installed?");
                return games;
            }

            string configurationPath = Path.Combine(GetInstallPath(), "cache", "configuration", "configurations");
            string assetsPath = Path.Combine(GetInstallPath(), "cache", "assets");


            //var yamlDeserializer = new StaticDeserializerBuilder(new Helpers.StaticContext())
            var yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            // Load data from the configurations file. This is the data the game+dlc that the user has access.
            // Not sure what happens if you are on a shared PC.
            var configurationFileData = await File.ReadAllBytesAsync(configurationPath);

            // This file contains multiple game records seperated by some custom header. 
            // We split this apart base on the methods from https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
            // and then return it as a list of games which we then check to see if it is in the installed list above.
            List<UbisoftRecord> configurationRecords = UbisoftConnectConfigurationParser.Parse(configurationFileData);
            foreach (UbisoftRecord configurationRecord in configurationRecords)
            {
                // TODO: Remove htis true.
                // Only bother trying to read the game data if the install list 
                if (installedTitles.ContainsKey(configurationRecord.InstallId))
                {
                    // Copy the yaml out for the game into a memory stream to load.
                    using (var memoryStream = new MemoryStream(configurationRecord.Size))
                    {
                        memoryStream.Write(configurationFileData, configurationRecord.Offset, configurationRecord.Size);
                        memoryStream.Position = 0;

                        using (var reader = new StreamReader(memoryStream))
                        {
                            try
                            {
                                UbisoftConnectConfigurationItem configurationItem = yamlDeserializer.Deserialize<UbisoftConnectConfigurationItem>(reader);
                                UbisoftConnectGame? extractedGame = ExtractGame(drives, assetsPath, installedTitles, configurationItem, configurationRecord);

                                if (extractedGame is null)
                                {
                                    continue;
                                }

                                await extractedGame.SaveToDatabaseAsync();

                                if (extractedGame.NeedsProcessing || forceNeedsProcessing)
                                {
                                    extractedGame.ProcessGame();
                                }

                                games.Add(extractedGame);
                            }
                            catch (Exception err)
                            {
                                Logger.Error(err, "Error loading UbisoftConnectConfigurationItem");
                            }
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
                    await cachedGame.DeleteAsync();
                }
            }

            return games;
        }

        string GetInstallPath()
        {
            if (string.IsNullOrEmpty(_installPath) == false)
            {
                return _installPath;
            }

            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var ubisoftConnectRegistryKey = hklm.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher"))
                    {
                        // if ubisoftConnectRegistryKey is null then Ubisoft Connect is not installed.
                        if (ubisoftConnectRegistryKey is null)
                        {
                            return string.Empty;
                        }

                        var installPath = ubisoftConnectRegistryKey.GetValue("InstallDir") as string;
                        if (string.IsNullOrEmpty(installPath) == false)
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
    }
}
