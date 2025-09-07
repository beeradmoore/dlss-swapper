using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.DLSS;
using NvAPIWrapper;
using NvAPIWrapper.DRS;

namespace DLSS_Swapper.Helpers;

internal class NVAPIHelper
{
    // Via https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
    const uint NGX_DLAA_OVERRIDE_ID = 0x10E41DF4;
    const uint NGX_DLSSG_MULTI_FRAME_COUNT_ID = 0x104D6667;
    const uint NGX_DLSS_FG_OVERRIDE_ID = 0x10E41E03;
    const uint NGX_DLSS_FG_OVERRIDE_RESERVED_KEY1_ID = 0x10C7D57E;
    const uint NGX_DLSS_FG_OVERRIDE_RESERVED_KEY2_ID = 0x10C7D519;
    const uint NGX_DLSS_OVERRIDE_OPTIMAL_SETTINGS_ID = 0x10AFB76C;
    const uint NGX_DLSS_RR_MODE_ID = 0x10BD9423;
    const uint NGX_DLSS_RR_OVERRIDE_ID = 0x10E41E02;
    const uint NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID = 0x10E41DF7;
    const uint NGX_DLSS_RR_OVERRIDE_RESERVED_KEY1_ID = 0x10C7D86C;
    const uint NGX_DLSS_RR_OVERRIDE_RESERVED_KEY2_ID = 0x10C7D597;
    const uint NGX_DLSS_RR_OVERRIDE_SCALING_RATIO_ID = 0x10C7D4A2;
    const uint NGX_DLSS_SR_MODE_ID = 0x10AFB768;
    const uint NGX_DLSS_SR_OVERRIDE_ID = 0x10E41E01;
    const uint NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID = 0x10E41DF3;
    const uint NGX_DLSS_SR_OVERRIDE_RESERVED_KEY1_ID = 0x10C7D684;
    const uint NGX_DLSS_SR_OVERRIDE_RESERVED_KEY2_ID = 0x10C7D82C;
    const uint NGX_DLSS_SR_OVERRIDE_SCALING_RATIO_ID = 0x10E41DF5;

    public bool Supported { get; init; }

    public IReadOnlyList<PresetOption> DlssPresetOptions { get; init; }

    public IReadOnlyList<PresetOption> DlssDPresetOptions { get; init; }

    public static NVAPIHelper Instance { get; private set; } = new NVAPIHelper();

    readonly DriverSettingsSession? _driverSettingSession;
    readonly Dictionary<string, DriverSettingsProfile> _cachedProfiles = new Dictionary<string, DriverSettingsProfile>();


    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    private NVAPIHelper()
    {
        // Load DLSS presets
        try
        {
            var dlssPresetsJsonPath = @"Assets\dlss_presets.json";
            if (File.Exists(dlssPresetsJsonPath) == true)
            {
                var dlssPresetOptions = JsonSerializer.Deserialize<List<PresetOption>>(File.ReadAllText(dlssPresetsJsonPath))?.Where(x => x.Used == true)?.ToList();
                if (dlssPresetOptions is not null && dlssPresetOptions.Count > 0)
                {
                    for (var i = 0; i < dlssPresetOptions.Count; ++i)
                    {
                        dlssPresetOptions[i].UpdateNameFromTranslation();
                    }
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
                new PresetOption(ResourceHelper.GetString("DLSS_Preset_Default"), 0x00000000),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "A"), 0x00000001),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "B"), 0x00000002),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "C"), 0x00000003),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "D"), 0x00000004),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "E"), 0x00000005),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "F"), 0x00000006),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "G"), 0x00000007),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "H"), 0x00000008),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "I"), 0x00000009),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "J"), 0x0000000A),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "K"), 0x0000000B),
                new PresetOption(ResourceHelper.GetString("DLSS_Preset_AlwaysUseLatest"), 0x00FFFFFF),
            ];
        }

        // Load DLSS D presets
        try
        {
            var dlssDPresetsJsonPath = @"Assets\dlss_d_presets.json";
            if (File.Exists(dlssDPresetsJsonPath) == true)
            {
                var dlssDPresetOptions = JsonSerializer.Deserialize<List<PresetOption>>(File.ReadAllText(dlssDPresetsJsonPath))?.Where(x => x.Used == true)?.ToList();
                if (dlssDPresetOptions is not null && dlssDPresetOptions.Count > 0)
                {
                    for (var i = 0; i < dlssDPresetOptions.Count; ++i)
                    {
                        if (dlssDPresetOptions[i].Value == 1 || dlssDPresetOptions[i].Value == 2 || dlssDPresetOptions[i].Value == 3)
                        {
                            dlssDPresetOptions[i].Deprecated = true;
                        }
                    }

                    DlssDPresetOptions = dlssDPresetOptions.AsReadOnly();
                }
                else
                {
                    throw new Exception("dlss_d_presets.json is empty or invalid.");
                }
            }
            else
            {
                throw new Exception("dlss_d_presets.json not found.");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not load dlss_presets.json, using default presets.");
            DlssDPresetOptions = [
                new PresetOption(ResourceHelper.GetString("DLSS_Preset_Default"), 0x00000000),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter_Deprecated", "A"), 0x00000001),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter_Deprecated", "B"), 0x00000002),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter_Deprecated", "C"), 0x00000003),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "D"), 0x00000004),
                new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "E"), 0x00000005),
                //new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "F"), 0x00000006),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "G"), 0x00000007),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "H"), 0x00000008),
                // new DlssPresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "I"), 0x00000009),
                //new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "J"), 0x0000000A),
                //new PresetOption(ResourceHelper.GetFormattedResourceTemplate("DLSS_Preset_Letter", "K"), 0x0000000B),
                //new PresetOption(ResourceHelper.GetString("DLSS_Preset_AlwaysUseLatest"), 0x00FFFFFF),
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

        LanguageManager.Instance.OnLanguageChanged += LanguageManager_OnLanguageChanged;
    }

    void LanguageManager_OnLanguageChanged()
    {
        foreach (var dlssPresetOption in DlssPresetOptions)
        {
            dlssPresetOption.UpdateNameFromTranslation();
        }

        foreach (var dlssDPresetOption in DlssDPresetOptions)
        {
            dlssDPresetOption.UpdateNameFromTranslation();
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

            if (_driverSettingSession.CurrentGlobalProfile.GetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID).CurrentValue is uint currentValue)
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

    /*
    public uint GetGlobalDLSSDPreset()
    {
        if (Supported == false || _driverSettingSession is null)
        {
            return 0;
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot get DLSS D preset.");
                return 0;
            }

            if (_driverSettingSession.CurrentGlobalProfile.GetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID).CurrentValue is uint currentValue)
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
    */
      
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

            _driverSettingSession.CurrentGlobalProfile.SetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not set setting for CurrentGlobalProfile.");
            return false;
        }
    }

    /*
    public bool SetGlobalDLSSDPreset(uint preset)
    {
        if (Supported == false || _driverSettingSession is null)
        {
            return false;
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot set DLSS D preset.");
                return false;
            }

            _driverSettingSession.CurrentGlobalProfile.SetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err, "Could not set setting for CurrentGlobalProfile.");
            return false;
        }
    }
    */


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
            var dlssPreset = settings.FirstOrDefault(x => x.SettingId == NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID);

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

    public uint GetGameDLSSDPreset(Game game)
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
            var dlssDPreset = settings.FirstOrDefault(x => x.SettingId == NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID);

            if (dlssDPreset is ProfileSetting profileSetting && profileSetting.CurrentValue is uint currentValue)
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
            gameProfile.SetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
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


    public bool SetGameDLSSDPreset(Game game, uint preset)
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
            gameProfile.SetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();
            game.DlssDPreset = preset;
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
            return false;
        }
    }
    
}
