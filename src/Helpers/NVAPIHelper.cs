using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.DLSS;
using NvAPIWrapper;
using NvAPIWrapper.DRS;

namespace DLSS_Swapper.Helpers;

internal class NVAPIHelper
{
    const uint OVERRIDE_DLSS_SR_PRESET_SETTING_ID = 0x10E41DF3;

    public bool Supported { get; init; }

    public IReadOnlyList<PresetOption> DlssPresetOptions { get; init; }

    public static NVAPIHelper Instance { get; private set; } = new NVAPIHelper();

    readonly DriverSettingsSession? _driverSettingSession;
    readonly Dictionary<string, DriverSettingsProfile> _cachedProfiles = new Dictionary<string, DriverSettingsProfile>();


    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    private NVAPIHelper()
    {
        try
        {
            var dlssPresetsJsonPath = @"Assets\dlss_presets.json";
            if (File.Exists(dlssPresetsJsonPath) == true)
            {
                var dlssPresetOptions = JsonSerializer.Deserialize<List<PresetOption>>(File.ReadAllText(dlssPresetsJsonPath))?.Where(x => x.Used == true)?.ToList();
                if (dlssPresetOptions is not null && dlssPresetOptions.Count > 0)
                {
                    DlssPresetOptions = dlssPresetOptions.AsReadOnly();
                }
                else
                {
                    throw new Exception("dlss_presets.json is empty or invalid.");
                }
            }
            else
            {
                throw new Exception("dlss_presets.json not found.");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not load dlss_presets.json, using default presets.");
            DlssPresetOptions = [
               new PresetOption("Default", 0x00000000),
                new PresetOption("Preset A", 0x00000001),
                new PresetOption("Preset B", 0x00000002),
                new PresetOption("Preset C", 0x00000003),
                new PresetOption("Preset D", 0x00000004),
                new PresetOption("Preset E", 0x00000005),
                new PresetOption("Preset F", 0x00000006),
                // new DlssPresetOption("Preset G", 0x00000007),
                // new DlssPresetOption("Preset H", 0x00000008),
                // new DlssPresetOption("Preset I", 0x00000009),
                new PresetOption("Preset J", 0x0000000A),
                new PresetOption("Preset K", 0x0000000B),
                new PresetOption("Always use latest", 0x00FFFFFF),
            ];
        }

        try
        {
            // Attempt to load nvapi64 before we try use NvAPIWrapper
            var handle = LoadLibrary("nvapi64.dll");
            if (handle == IntPtr.Zero)
            {
                // Could not load nvapi64.dll, NVIDIA drivers are not installed.
            }
            else
            {
                // Library should exist, unload it and continue
                FreeLibrary(handle);

                NVIDIA.Initialize();

                _driverSettingSession = DriverSettingsSession.CreateAndLoad();
                // Looked at checking if it is worth filtering this by IsValid and GPUSupport.IsGeForceSupported,
                // but it only dropped number of items from 7200 to 7000.
                _cachedProfiles = _driverSettingSession.Profiles.AsParallel().ToDictionary(profile => profile.Name);
                Supported = true;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "If you don't have an NVIDIA card this is expected and can be ignored.");
        }
    }

    public DriverSettingsProfile? FindGameProfile(Game game)
    {
        // If this is cached on the game use it first.
        if (game.DriverSettingsProfile is not null)
        {
            return game.DriverSettingsProfile;
        }

        // If this is directly accessed from the cached profiles list it there. 
        if (_cachedProfiles.TryGetValue(game.Title, out var exactProfile))
        {
            game.DriverSettingsProfile = exactProfile;
            return exactProfile;
        }

        // If the game is not a direct title match we will search through all profiles for a match.
        // First sort all possible profiles by title similarity.
        var possibleProfiles = _cachedProfiles.OrderBy(entry => CommonHelpers.LevenshteinDistance(entry.Key, game.Title)).Select(x => x.Value).ToList();

        // Now list every executable in the game install folder
        var executables = Directory.GetFiles(game.InstallPath, "*.exe", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToList();

        // For each possible profile (starting with most likely) we try confirm the profile.
        foreach (var possibleProfile in possibleProfiles)
        {
            // To be sure we confirm the profile we also check applications in the profile to match our executables.
            foreach (var application in possibleProfile.Applications)
            {
                if (executables.Contains(application.ApplicationName, StringComparer.OrdinalIgnoreCase))
                {
                    // If matched, cache 
                    game.DriverSettingsProfile = possibleProfile;
                    return possibleProfile;
                }
            }
        }

        return null;
    }

    public uint GetGlobalDLSSPreset()
    {
        if (Supported == false || _driverSettingSession is null)
        {
            return 0;
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot get DLSS preset.");
                return 0;
            }

            if (_driverSettingSession.CurrentGlobalProfile.GetSetting(OVERRIDE_DLSS_SR_PRESET_SETTING_ID).CurrentValue is uint currentValue)
            {
                return currentValue;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not get setting for CurrentGlobalProfile.");
            return 0;
        }

        return 0;
    }

    public bool SetGlobalDLSSPreset(uint preset)
    {
        if (Supported == false || _driverSettingSession is null)
        {
            return false;
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot set DLSS preset.");
                return false;
            }

            _driverSettingSession.CurrentGlobalProfile.SetSetting(OVERRIDE_DLSS_SR_PRESET_SETTING_ID, preset);
            _driverSettingSession.Save();
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not set setting for CurrentGlobalProfile.");
            return false;
        }
    }


    public uint GetGameDLSSPreset(Game game)
    {
        if (Supported == false)
        {
            return 0;
        }

        try
        {
            var closestProfile = FindGameProfile(game);
            if (closestProfile is null)
            {
                return 0;
            }
            var settings = closestProfile.Settings;
            var dlssPreset = settings.FirstOrDefault(x => x.SettingId == OVERRIDE_DLSS_SR_PRESET_SETTING_ID);

            if (dlssPreset is ProfileSetting profileSetting && profileSetting.CurrentValue is uint currentValue)
            {
                return currentValue;
            }
            else
            {
                return 0;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
            return 0;
        }
    }

    public bool SetGameDLSSPreset(Game game, uint preset)
    {
        if (Supported == false || _driverSettingSession is null)
        {
            return false;
        }

        try
        {
            var gameProfile = FindGameProfile(game);
            if (gameProfile is null)
            {
                throw new Exception($"There was an error finding a matching profile for game \"{game.Title}\"");
            }
            gameProfile.SetSetting(OVERRIDE_DLSS_SR_PRESET_SETTING_ID, preset);
            _driverSettingSession.Save();
            game.DlssPreset = preset;
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
            return false;
        }
    }
}
