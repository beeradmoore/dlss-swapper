using System;
using System.Collections.Generic;
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

            var configurationPath = Path.Combine(GetInstallPath(), "cache", "configuration", "configurations");
            var assetsPath = Path.Combine(GetInstallPath(), "cache", "assets");
            var configurationData = await File.ReadAllLinesAsync(configurationPath);

            var lineNumber = 0;
            var stringBuilder = new StringBuilder();
            var isReadingData = false;
            var foundItems = 0;


            var yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            foreach (var line in configurationData)
            {
                if (line.EndsWith("version: 2.0"))
                {
                    isReadingData = true;
                    stringBuilder.Clear();
                    stringBuilder.AppendLine("version: 2.0");
                }
                else if (line.StartsWith("root:") || line.StartsWith("localizations:") || line.StartsWith("  "))
                {
                    stringBuilder.AppendLine(line);
                }
                else
                {
                    if (stringBuilder.Length == 0)
                    {

                    }
                    else
                    {
                        isReadingData = false;
                        var data = stringBuilder.ToString();
                        ++foundItems;
                        stringBuilder.Clear();

                        try
                        {
                            var ubisoftConnectConfigurationItem = yamlDeserializer.Deserialize<UbisoftConnectConfigurationItem>(data);

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


                            // TODO: async/yield in the future?
                            var uniqueExecutables = ubisoftConnectConfigurationItem.Root.StartGame.GetUniqueExecutables();
                            if (uniqueExecutables.Count == 1)
                            {
                                var gameInstallPath = GetInstallPathFromRegister(uniqueExecutables[0].WorkingDirectory.Register);
                                // So close, but there is no game install path.
                                if (String.IsNullOrEmpty(gameInstallPath))
                                {
                                    continue;
                                }
                                var game = new UbisoftConnectGame(localImage, remoteImage)
                                {
                                    Title = ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier,
                                    InstallPath = gameInstallPath,
                                };
                                game.DetectDLSS();
                                games.Add(game);
                            }
                            else if (uniqueExecutables.Count > 1)
                            {
                                foreach (var uniqueExecutable in uniqueExecutables)
                                {
                                    var gameInstallPath = GetInstallPathFromRegister(uniqueExecutable.WorkingDirectory.Register);
                                    // So close, but there is no game install path.
                                    if (String.IsNullOrEmpty(gameInstallPath))
                                    {
                                        continue;
                                    }

                                    if (Directory.Exists(gameInstallPath) == false)
                                    {
                                        continue;
                                    }

                                    var title = ubisoftConnectConfigurationItem.Root.Installer.GameIdentifier;

                                    // If there is a unique shortcut name we should use it.
                                    if (String.IsNullOrEmpty(uniqueExecutable.ShortcutName) == false)
                                    {
                                        // Check if its in localizations, otherwise it probably isn't a key and we should just use it.
                                        if (ubisoftConnectConfigurationItem.Localizations?.ContainsKey("default") == true && ubisoftConnectConfigurationItem.Localizations["default"]?.ContainsKey(uniqueExecutable.ShortcutName) == true)
                                        {
                                            title = ubisoftConnectConfigurationItem.Localizations["default"][uniqueExecutable.ShortcutName];
                                        }
                                        else
                                        {
                                            title = uniqueExecutable.ShortcutName;
                                        }
                                    }

                                    var game = new UbisoftConnectGame(localImage, remoteImage)
                                    {
                                        Title = title,
                                        InstallPath = gameInstallPath,
                                    };

                                    game.DetectDLSS();
                                    games.Add(game);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.Error($"Error parsing Ubisoft Connect element. ({err.Message})");
                        }
                    }
                }
                ++lineNumber;
            }


            games.Sort();
            _loadedGames.AddRange(games);
            _loadedDLSSGames.AddRange(games.Where(g => g.HasDLSS == true));

            return games;
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
