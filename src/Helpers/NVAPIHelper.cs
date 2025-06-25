using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ABI.Windows.ApplicationModel.Activation;
using DLSS_Swapper.Data;
using NvAPIWrapper.DRS;

namespace DLSS_Swapper.Helpers
{

    internal class NVAPIHelper
    {
        private static readonly Dictionary<string, uint> PresetMap = new Dictionary<string, uint>
        {
            ["Default"] = 0x00000000,
            ["Preset J"] = 0x0000000A,
            ["Preset K"] = 0x0000000B,
        };

        public List<DlssPresetOption> DlssPresetOptions { get; } = PresetMap
        .Select(kvp => new DlssPresetOption
        {
            Value = $"0x{kvp.Value:X8}",
            Label = kvp.Key == "Preset 0" ? "Default" : kvp.Key
        })
        .ToList();

        public static NVAPIHelper Instance { get; private set; } = new NVAPIHelper();
        private readonly NvAPIWrapper.DRS.DriverSettingsSession nvapiSession;
        private Dictionary<string, DriverSettingsProfile> cachedProfiles = new Dictionary<string, DriverSettingsProfile>();

        private NVAPIHelper()
        {
            nvapiSession = NvAPIWrapper.DRS.DriverSettingsSession.CreateAndLoad();
            cachedProfiles = nvapiSession.Profiles.AsParallel().ToDictionary(profile => profile.Name);
            Logger.Info("" + cachedProfiles.First());
        }

        private DriverSettingsProfile FindClosestTitle(string title)
        {
            if (cachedProfiles.TryGetValue(title, out var exactProfile))
                return exactProfile;
            return cachedProfiles
                .OrderBy(entry => CommonHelpers.LevenshteinDistance(entry.Key, title))
                .Select(entry => entry.Value)
                .FirstOrDefault();
        }

        public string GetGameDLSSPreset(string title)
        {
            try
            {
                var closestProfile = FindClosestTitle(title);
                if (closestProfile is null)
                {
                    return "0x00000000";
                }
                var settings = closestProfile.Settings;
                var dlssPreset = settings.FirstOrDefault(x => x.SettingId == 283385331);

                if (dlssPreset is not null)
                {
                    return $"0x{dlssPreset.CurrentValue:X8}";

                }
                else
                {
                    return "0x00000000";
                }

            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
                return "0x00000000";
            }
        }

        public void SetGameDLSSPreset(Game game)
        {
            try
            {
                var closestProfile = FindClosestTitle(game.Title);
                if (closestProfile is null)
                {
                    throw new Exception("There was an error finding a matching Profile");
                }
                var preset = game.DlssPreset;
                if (preset.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                {
                    preset = preset.Substring(2);
                }

                uint value = uint.Parse(preset, NumberStyles.HexNumber);
                closestProfile.SetSetting(283385331, value);
                nvapiSession.Save();
                Logger.Info("Set Profile for: " + game.Title + " to " + game.DlssPreset);
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}

public class DlssPresetOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
