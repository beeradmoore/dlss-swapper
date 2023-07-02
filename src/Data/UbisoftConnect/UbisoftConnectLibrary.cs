using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            internal string InstallPath { get; init; } = String.Empty;
        }


        public GameLibrary GameLibrary => GameLibrary.UbisoftConnect;
        public string Name => "Ubisoft Connect";

        List<Game> _loadedGames = new List<Game>();
        public List<Game> LoadedGames { get { return _loadedGames; } }

        List<Game> _loadedDLSSGames = new List<Game>();
        public List<Game> LoadedDLSSGames { get { return _loadedDLSSGames; } }

        string _installPath = String.Empty;

        public bool IsInstalled()
        {
            return String.IsNullOrEmpty(GetInstallPath()) == false;
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


            var installs = new Dictionary<int, UbisoftGameRegistryRecord>();

            try
            {
                // Only focused on x64 machines.
                using (var ubisoftConnectInstallsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs"))
                {
                    if (ubisoftConnectInstallsKey == null)
                    {
                        throw new Exception("Could not detect ubisoftConnectInstallsKey");
                    }
                    // if ubisoftConnectRegistryKey is null then steam is not installed.
                    var subKeyNames = ubisoftConnectInstallsKey.GetSubKeyNames();
                    foreach (var subKeyName in subKeyNames)
                    {
                        if (Int32.TryParse(subKeyName, out var installId))
                        {
                            using (var ubisoftConnectInstallDirKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\{subKeyName}"))
                            {
                                if (ubisoftConnectInstallDirKey == null)
                                {
                                    break;
                                }

                                var gameInstallDir = ubisoftConnectInstallDirKey?.GetValue("InstallDir") as String;
                                if (String.IsNullOrEmpty(gameInstallDir) == false)
                                {
                                    installs[installId] = new UbisoftGameRegistryRecord()
                                    {
                                        InstallId = installId,
                                        InstallPath = gameInstallDir,
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error($"Error getting list of installs: {err.Message}");
            }

            var configurationPath = Path.Combine(GetInstallPath(), "cache", "configuration", "configurations");
            var assetsPath = Path.Combine(GetInstallPath(), "cache", "assets");

            var yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var configurationFileData = await File.ReadAllBytesAsync(configurationPath);

            var configurationRecords = ParseConfiguration(configurationFileData);
            foreach (var configurationRecord in configurationRecords)
            {
                if (installs.ContainsKey(configurationRecord.InstallId))
                {
                    using (var memoryStream = new MemoryStream(configurationRecord.Size))
                    {
                        memoryStream.Write(configurationFileData, configurationRecord.Offset, configurationRecord.Size);
                        memoryStream.Position = 0;
                        using (var reader = new StreamReader(memoryStream))
                        {
                            try
                            {
                                var ubisoftConnectConfigurationItem = yamlDeserializer.Deserialize<UbisoftConnectConfigurationItem>(reader);

                                // This is bad.
                                if (ubisoftConnectConfigurationItem == null || ubisoftConnectConfigurationItem.Version == null || ubisoftConnectConfigurationItem.Root == null)
                                {
                                    Logger.Error("Could not load Ubisoft Connect item.");
                                    continue;
                                }

                                // If Version isn't 2.0 we will just not load any games.
                                if (ubisoftConnectConfigurationItem.Version != "2.0")
                                {
                                    Logger.Error($"Unknown item version. Expected 2.0, found {ubisoftConnectConfigurationItem.Version}");
                                    continue;
                                }

                                // This can be expected. If there is no installer item there is no game to install.
                                if (ubisoftConnectConfigurationItem.Root.Installer == null)
                                {
                                    continue;
                                }

                                // This is not expected. 
                                if (ubisoftConnectConfigurationItem.Root.StartGame == null)
                                {
                                    Logger.Error($"StartGameNode is null for {ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier}.");
                                    continue;
                                }


                                var localImage = String.Empty;
                                var remoteImage = String.Empty;
                                if (ubisoftConnectConfigurationItem.Root.LogoImage != null)
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

                                var game = new UbisoftConnectGame(localImage, remoteImage)
                                {
                                    Title = ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier,
                                    InstallPath = installs[configurationRecord.InstallId].InstallPath,
                                };
                                game.DetectDLSS();
                                games.Add(game);
                            }
                            catch (Exception err)
                            {
                                Logger.Error($"Error loading UbisoftConnectConfigurationItem: {err.Message}");
                            }
                        }
                    }
                }
            }

            games.Sort();
            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

            return games;
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

        string GetInstallPathFromRegister(string register)
        {
            if (String.IsNullOrEmpty(register))
            {
                return String.Empty;
            }

            var registerParts = register.Split("\\");
            if (registerParts.Length == 0)
            {
                return String.Empty;
            }
            var registryHive = registerParts[0] switch
            {
                "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
                "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
                _ => RegistryHive.PerformanceData, // This should never be where a game is installed. So we use this to abort.
            };

            if (registryHive == RegistryHive.PerformanceData)
            {
                return String.Empty;
            }

            using (var baseKey = RegistryKey.OpenBaseKey(registryHive, RegistryView.Registry32))
            {
                if (baseKey == null)
                {
                    return String.Empty;
                }

                var targetSubKey = String.Join("\\", registerParts[1..^1]);
                using (var subKey = baseKey.OpenSubKey(targetSubKey, false))
                {
                    if (subKey == null)
                    {
                        return String.Empty;
                    }

                    return subKey.GetValue(registerParts.Last(), String.Empty) as String ?? String.Empty;
                }
            }    
        }

        string GetInstallPath()
        {
            if (String.IsNullOrEmpty(_installPath) == false)
            {
                return _installPath;
            }

            try
            {
                // Only focused on x64 machines.
                using (var ubisoftConnectRegistryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher"))
                {
                    if (ubisoftConnectRegistryKey == null)
                    {
                        return String.Empty;
                    }
                    // if ubisoftConnectRegistryKey is null then steam is not installed.
                    var installPath = ubisoftConnectRegistryKey?.GetValue("InstallDir") as String;
                    if (String.IsNullOrEmpty(installPath) == false)
                    {
                        _installPath = installPath;
                    }

                    return _installPath ?? String.Empty;
                }
            }
            catch (Exception err)
            {
                _installPath = String.Empty;
                Logger.Error(err.Message);
                return String.Empty;
            }
        }        
    }
}
