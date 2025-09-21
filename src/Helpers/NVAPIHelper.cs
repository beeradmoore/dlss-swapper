using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.DLSS;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NvAPIWrapper;
using NvAPIWrapper.DRS;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.General;

namespace DLSS_Swapper.Helpers;

record NVAPIResult<T>
{
    public bool Success { get; init; }
    public T Result { get; init; }
    public Status? Status { get; init; }

    public NVAPIResult(bool success, T result, Status? status = null)
    {
        Success = success;
        Result = result;
        Status = status;
    }
}

public class NVIDIAApiException : Exception
{
    public override string Message => GeneralApi.GetErrorMessage(Status) ?? Status.ToString();

    //
    // Summary:
    //     Gets NVIDIA Api exception status code
    public Status Status { get; }

    internal NVIDIAApiException(Status status)
    {
        Status = status;
    }
}

internal partial class NVAPIHelper : ObservableObject
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

    [ObservableProperty]
    public partial bool IsSupported { get; set; }

    [ObservableProperty]
    public partial bool PermissionIssue { get; set; }

    public IReadOnlyList<PresetOption> DlssPresetOptions { get; init; }

    public IReadOnlyList<PresetOption> DlssDPresetOptions { get; init; }

    public static NVAPIHelper Instance { get; private set; } = new NVAPIHelper();

    readonly DriverSettingsSession? _driverSettingSession;
    readonly Dictionary<string, DriverSettingsProfile> _cachedProfiles = new Dictionary<string, DriverSettingsProfile>();

    Status? _lastErrorStatus;

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

                // TODO: This takes ~400ms on my dev machine (in debug mode)
                // Time this in release mode and see if it can be done better (eg, of main thread so it isn't blocking)
                // It may also pay to do initilize this earlier, as currently this will all run when
                // you click your first game.
                _cachedProfiles = _driverSettingSession.Profiles.AsParallel().ToDictionary(profile => profile.Name);
                IsSupported = true;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "If you don't have an NVIDIA card this is expected and can be ignored.");
        }

        LanguageManager.Instance.OnLanguageChanged += LanguageManager_OnLanguageChanged;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(PermissionIssue))
        {
            // If a permission issue arises change IsSupported to false.
            if (PermissionIssue == true)
            {
                IsSupported = false;
            }
        }
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

    public NVAPIResult<uint> GetGlobalDLSSPreset()
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<uint>(false, 0);
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot get DLSS preset.");
                return new NVAPIResult<uint>(false, 0);
            }

            if (_driverSettingSession.CurrentGlobalProfile.GetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID).CurrentValue is uint currentValue)
            {
                return new NVAPIResult<uint>(true, currentValue);
            }

            // No default value found.
            return new NVAPIResult<uint>(true, 0);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not get setting for GetGlobalDLSSPreset. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Could not get setting for GetGlobalDLSSPreset.");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0);
        }
    }

    public NVAPIResult<uint> GetGlobalDLSSDPreset()
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<uint>(false, 0);
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot get DLSS D preset.");
                return new NVAPIResult<uint>(false, 0);
            }

            if (_driverSettingSession.CurrentGlobalProfile.GetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID).CurrentValue is uint currentValue)
            {
                return new NVAPIResult<uint>(true, currentValue);
            }

            // No default value found.
            return new NVAPIResult<uint>(true, 0);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not get setting for GetGlobalDLSSDPreset. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Could not get setting for GetGlobalDLSSDPreset.");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0);
        }
    }

    public NVAPIResult<bool> SetGlobalDLSSPreset(uint preset)
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<bool>(false, false);
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot set DLSS preset.");
                return new NVAPIResult<bool>(false, false);
            }

            _driverSettingSession.CurrentGlobalProfile.SetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();

            return new NVAPIResult<bool>(true, true);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not set setting for SetGlobalDLSSPreset with preset {preset}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not set setting for SetGlobalDLSSPreset with preset {preset}.");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false);
        }
    }

    public NVAPIResult<bool> SetGlobalDLSSDPreset(uint preset)
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<bool>(false, false);
        }

        try
        {
            if (_driverSettingSession.CurrentGlobalProfile is null)
            {
                Logger.Error("Current global profile is null, cannot set DLSS D preset.");
                return new NVAPIResult<bool>(false, false);
            }

            _driverSettingSession.CurrentGlobalProfile.SetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();

            return new NVAPIResult<bool>(true, true);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not set setting for SetGlobalDLSSDPreset with preset {preset}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not set setting for SetGlobalDLSSDPreset with preset {preset}.");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false);
        }
    }

    public NVAPIResult<uint> GetGameDLSSPreset(Game game)
    {
        if (IsSupported == false)
        {
            return new NVAPIResult<uint>(false, 0);
        }

        try
        {
            var closestProfile = FindGameProfile(game);
            if (closestProfile is null)
            {
                Logger.Error($"Could not find profile for game {game.Title}.");
                return new NVAPIResult<uint>(false, 0);
            }

            var settings = closestProfile.Settings;
            var dlssPreset = settings.FirstOrDefault(x => x.SettingId == NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID);

            if (dlssPreset is ProfileSetting profileSetting && profileSetting.CurrentValue is uint currentValue)
            {
                return new NVAPIResult<uint>(true, currentValue);
            }

            // No default value found.
            return new NVAPIResult<uint>(true, 0);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not get setting for GetGameDLSSPreset for game {game.Title}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not get setting for GetGameDLSSPreset for game {game.Title}.");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0);
        }
    }

    public NVAPIResult<uint> GetGameDLSSDPreset(Game game)
    {
        if (IsSupported == false)
        {
            return new NVAPIResult<uint>(false, 0);
        }

        try
        {
            var closestProfile = FindGameProfile(game);
            if (closestProfile is null)
            {
                Logger.Error($"Could not find profile for game {game.Title}.");
                return new NVAPIResult<uint>(false, 0);
            }

            var settings = closestProfile.Settings;
            var dlssDPreset = settings.FirstOrDefault(x => x.SettingId == NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID);

            if (dlssDPreset is ProfileSetting profileSetting && profileSetting.CurrentValue is uint currentValue)
            {
                return new NVAPIResult<uint>(true, currentValue);
            }

            // No default value found.
            return new NVAPIResult<uint>(true, 0);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not get setting for GetGameDLSSDPreset for game {game.Title}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not get setting for GetGameDLSSDPreset for game {game.Title}.");
            Debugger.Break();
            return new NVAPIResult<uint>(false, 0);
        }
    }

    public NVAPIResult<bool> SetGameDLSSPreset(Game game, uint preset)
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<bool>(false, false);
        }

        try
        {
            var gameProfile = FindGameProfile(game);
            if (gameProfile is null)
            {
                Logger.Error($"Could not find profile for game {game.Title}.");
                return new NVAPIResult<bool>(false, false);
            }

            gameProfile.SetSetting(NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();

            game.DlssPreset = preset;

            return new NVAPIResult<bool>(true, true);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not set setting for SetGameDLSSPreset for game {game.Title} with preset {preset}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not set setting for SetGameDLSSPreset for game {game.Title} with preset {preset}.");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false);
        }
    }

    public NVAPIResult<bool> SetGameDLSSDPreset(Game game, uint preset)
    {
        if (IsSupported == false || _driverSettingSession is null)
        {
            return new NVAPIResult<bool>(false, false);
        }

        try
        {
            var gameProfile = FindGameProfile(game);
            if (gameProfile is null)
            {
                Logger.Error($"Could not find profile for game {game.Title}.");
                return new NVAPIResult<bool>(false, false);
            }

            gameProfile.SetSetting(NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION_ID, preset);
            _driverSettingSession.Save();

            game.DlssDPreset = preset;

            return new NVAPIResult<bool>(true, false);
        }
        catch (NVIDIAApiException ex)
        {
            _lastErrorStatus = ex.Status;
            if (ex.Status == Status.InvalidUserPrivilege)
            {
                PermissionIssue = true;
            }
            Logger.Error(ex, $"Could not set setting for SetGameDLSSDPreset for game {game.Title} with preset {preset}. ({ex.Status})");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false, ex.Status);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not set setting for SetGameDLSSDPreset for game {game.Title} with preset {preset}.");
            Debugger.Break();
            return new NVAPIResult<bool>(false, false);
        }
    }

    public async Task DisplayNVAPIErrorAsync(XamlRoot xamlRoot)
    {
        var dialog = new EasyContentDialog(xamlRoot)
        {
            Title = ResourceHelper.GetString("GamePage_NVAPIError_Title"),
            PrimaryButtonText = ResourceHelper.GetString("General_Okay"),
            SecondaryButtonText = ResourceHelper.GetString("General_OpenLogsDirectory"),
            DefaultButton = ContentDialogButton.Primary,
            Content = ResourceHelper.GetString("GamePage_NVAPIError_Message"),
        };

        if (_lastErrorStatus is not null)
        {
            dialog.Title = $"{dialog.Title} - {_lastErrorStatus}";
        }

        if (Environment.IsPrivilegedProcess)
        {
            dialog.Content = ResourceHelper.GetString("GamePage_NVAPIError_Admin_Message");
        }

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            FileSystemHelper.OpenFolderInExplorer(Logger.LogDirectory);
        }
    }
}
