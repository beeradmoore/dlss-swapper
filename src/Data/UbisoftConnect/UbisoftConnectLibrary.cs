using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.Win32;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    internal class UbisoftConnectLibrary : IGameLibrary
    {
        private record UbisoftRecord
        {
            internal int Size { get; init; } = 0;
            internal int Offset { get; init; } = 0;
            internal int InstallId { get; init; } = 0;
            internal int LaunchId { get; init; } = 0;
        }

        private record UbisoftGameRegistryRecord
        {
            internal int InstallId { get; init; } = 0;
            internal string InstallPath { get; init; } = string.Empty;
        }


        public GameLibrary GameLibrary => GameLibrary.UbisoftConnect;
        public string Name => "Ubisoft Connect";

        public Type GameType => typeof(UbisoftConnectGame);

        static UbisoftConnectLibrary? instance = null;
        public static UbisoftConnectLibrary Instance => instance ??= new UbisoftConnectLibrary();

        GameLibrarySettings? _gameLibrarySettings;
        public GameLibrarySettings? GameLibrarySettings => _gameLibrarySettings ??= GameManager.Instance.GetGameLibrarySettings(GameLibrary);

        private UbisoftConnectLibrary()
        {

        }

        string _installPath = string.Empty;

        public bool IsInstalled()
        {
            return string.IsNullOrEmpty(GetInstallPath()) == false;
        }


        public async Task<List<Game>> ListGamesAsync(bool forceNeedsProcessing = false)
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
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var ubisoftConnectInstallsKey = hklm.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher\Installs"))
                    {
                        // if ubisoftConnectRegistryKey is null then Ubisoft is not installed .
                        if (ubisoftConnectInstallsKey is null)
                        {
                            throw new Exception("Could not detect ubisoftConnectInstallsKey.");
                        }

                        var subKeyNames = ubisoftConnectInstallsKey.GetSubKeyNames();
                        foreach (var subKeyName in subKeyNames)
                        {
                            // Only use the subKeyName that is a number (which is the installId.
                            if (Int32.TryParse(subKeyName, out var installId))
                            {
                                using (var ubisoftConnectInstallDirKey = ubisoftConnectInstallsKey.OpenSubKey(subKeyName))
                                {
                                    if (ubisoftConnectInstallDirKey is null)
                                    {
                                        break;
                                    }

                                    var gameInstallDir = ubisoftConnectInstallDirKey.GetValue("InstallDir") as string;
                                    if (string.IsNullOrEmpty(gameInstallDir) == false)
                                    {
                                        installedTitles[installId] = new UbisoftGameRegistryRecord()
                                        {
                                            InstallId = installId,
                                            InstallPath = PathHelpers.NormalizePath(gameInstallDir),
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
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

            var configurationPath = Path.Combine(GetInstallPath(), "cache", "configuration", "configurations");
            var assetsPath = Path.Combine(GetInstallPath(), "cache", "assets");


            //var yamlDeserializer = new StaticDeserializerBuilder(new Helpers.StaticContext())
            var yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            // Load data from the configurations file. This is the data the game+dlc that the user has access.
            // Not sure what happens if you are on a shared PC.
            var configurationFileData = await File.ReadAllBytesAsync(configurationPath).ConfigureAwait(false);

            // This file contains multiple game records seperated by some custom header.
            // We split this apart base on the methods from https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
            // and then return it as a list of games which we then check to see if it is in the installed list above.
            var configurationRecords = ParseConfiguration(configurationFileData);
            foreach (var configurationRecord in configurationRecords)
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
                                var ubisoftConnectConfigurationItem = yamlDeserializer.Deserialize<UbisoftConnectConfigurationItem>(reader);

                                // This is less than ideal. This usually happens with DLC or pre-orders.
                                if (ubisoftConnectConfigurationItem is null || ubisoftConnectConfigurationItem.Root is null)
                                {
                                    Logger.Info("Could not load Ubisoft Connect item. This is sometimes expected for certain titles.");
                                    continue;
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
                                    continue;
                                }

                                // This is not expected.
                                if (ubisoftConnectConfigurationItem.Root.StartGame is null)
                                {
                                    Logger.Info($"StartGameNode is null for {ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier}. This is likely a region specific installer.");
                                    continue;
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

                                var cachedGame = GameManager.Instance.GetGame<UbisoftConnectGame>(configurationRecord.InstallId.ToString());
                                var activeGame = cachedGame ?? new UbisoftConnectGame(configurationRecord.InstallId.ToString());
                                activeGame.Title = ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier;  // TODO: Will this be a problem if the game is already loaded
                                activeGame.InstallPath = PathHelpers.NormalizePath(installedTitles[configurationRecord.InstallId].InstallPath);
                                activeGame.LocalHeaderImage = localImage;
                                activeGame.RemoteHeaderImage = remoteImage;

                                if (activeGame.IsInIgnoredPath())
                                {
                                    continue;
                                }

                                if (Directory.Exists(activeGame.InstallPath) == false)
                                {
                                    Logger.Warning($"{Name} library could not load game {activeGame.Title} ({activeGame.PlatformId}) because install path does not exist: {activeGame.InstallPath}");
                                    continue;
                                }

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

        // Based on the methods from
        // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
        List<UbisoftRecord> ParseConfiguration(byte[] configurationFileData)
        {
            var configurationContent = new ReadOnlySpan<byte>(configurationFileData);

            var globalOffset = 0;
            var records = new List<UbisoftRecord>();

            try
            {
                while (globalOffset < configurationContent.Length)
                {
                    var data = configurationContent.Slice(globalOffset);

                    var configHeaderResult = ParseConfigurationHeader(data);
                    var objectSize = configHeaderResult.objectSize;
                    var installId = configHeaderResult.installId;
                    var launchId = configHeaderResult.launchId;
                    var headerSize = configHeaderResult.headerSize;

                    launchId = (launchId == 0 || launchId == installId) ? installId : launchId;

                    if (objectSize > 500)
                    {
                        records.Add(new UbisoftRecord()
                        {
                            Size = objectSize,
                            Offset = globalOffset + headerSize,
                            InstallId = installId,
                            LaunchId = launchId,
                        });
                    }

                    var global_offset_tmp = globalOffset;
                    globalOffset += objectSize + headerSize;



                    if (globalOffset < configurationContent.Length && configurationContent[globalOffset] != 0x0A)
                    {
                        var result = ParseConfigurationHeader(data, true);
                        objectSize = result.objectSize;
                        headerSize = result.headerSize;
                        globalOffset = global_offset_tmp + objectSize + headerSize;
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Warning($"parse_configuration failed with exception. Possibly 'configuration' file corrupted. - {err.Message}");
                Debugger.Break();
            }

            return records;
        }

        // Based on the methods from
        // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
        (int objectSize, int installId, int launchId, int headerSize) ParseConfigurationHeader(ReadOnlySpan<byte> header, bool secondEight = false)
        {

            try
            {
                var offset = 1; ;
                var multiplier = 1;
                var recordSize = 0;
                var tmpSize = 0;

                if (secondEight)
                {
                    while (header[offset] != 0x08 || (header[offset] == 0x08 && header[offset + 1] == 0x08))
                    {
                        recordSize += header[offset] * multiplier;
                        multiplier *= 256;
                        offset += 1;
                        tmpSize += 1;
                    }
                }
                else
                {
                    while (header[offset] != 0x08 || recordSize == 0)
                    {
                        recordSize += header[offset] * multiplier;
                        multiplier *= 256;
                        offset += 1;
                        tmpSize += 1;
                    }
                }

                recordSize = ConvertData(recordSize);

                offset += 1; // skip 0x08

                // look for launch_id
                multiplier = 1;
                var launchId = 0;


                while (header[offset] != 0x10 || header[offset + 1] == 0x10)
                {
                    launchId += header[offset] * multiplier;
                    multiplier *= 256;
                    offset += 1;
                }

                launchId = ConvertData(launchId);

                offset += 1; // skip 0x10

                multiplier = 1;
                var launchId2 = 0;

                while (header[offset] != 0x1A || (header[offset] == 0x1A && header[offset + 1] == 0x1A))
                {
                    launchId2 += header[offset] * multiplier;
                    multiplier *= 256;
                    offset += 1;
                }

                launchId2 = ConvertData(launchId2);

                // if object size is smaller than 128b, there might be a chance that secondary size will not occupy 2b
                //if record_size - offset < 128 <= record_size:
                if (recordSize - offset < 128 && 120 <= recordSize)
                {
                    tmpSize -= 1;
                    recordSize += 1;
                }
                // we end up in the middle of header, return values normalized
                // to end of record as well real yaml size and game launch_id
                return (recordSize - offset, launchId, launchId2, offset + tmpSize + 1);
            }
            catch (Exception err)
            {
                Logger.Warning($"ParseConfigurationHeader Error: {err.Message}");
                // something went horribly wrong, do not crash it,
                // just return 0s, this way it will be handled later in the code
                // 10 is to step a little in configuration file in order to find next game
                return (0, 0, 0, 10);
            }
        }

        // Based on the methods from
        // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
        int ConvertData(int data)
        {
            //calculate object size (konrad's formula)
            if (data > 256 * 256)
            {
                data = data - (128 * 256 * (int)Math.Ceiling((data / (256.0 * 256.0))));
                data = data - (128 * (int)Math.Ceiling((data / 256.0)));
            }
            else
            {
                if (data > 256)
                {
                    data = data - (128 * (int)Math.Ceiling(data / 256.0));
                }
            }
            return data;
        }

        public async Task LoadGamesFromCacheAsync()
        {
            try
            {
                UbisoftConnectGame[] games;
                using (await Database.Instance.Mutex.LockAsync())
                {
                    games = await Database.Instance.Connection.Table<UbisoftConnectGame>().ToArrayAsync().ConfigureAwait(false);
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
