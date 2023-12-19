using DLSS_Swapper.Data;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Interfaces
{
    [Flags]
    public enum GameLibrary : uint
    {
        Steam = 1,
        GOG = 2,
        EpicGamesStore = 4,
        UbisoftConnect = 8,
        XboxApp = 16,
        //SelfManaged = 32,
    };

    public interface IGameLibrary
    {
        GameLibrary GameLibrary { get; }
        string Name { get; }
        List<Game> LoadedGames { get; }
        List<Game> LoadedDLSSGames { get; }

        Task<List<Game>> ListGamesAsync();
        bool IsInstalled();

        static IGameLibrary GetGameLibrary(GameLibrary gameLibrary)
        {
            return gameLibrary switch
            {
                GameLibrary.Steam => new SteamLibrary(),
                GameLibrary.GOG => new GOGLibrary(),
                GameLibrary.EpicGamesStore => new EpicGamesStoreLibrary(),
                GameLibrary.UbisoftConnect => new UbisoftConnectLibrary(),
                GameLibrary.XboxApp => new XboxLibrary(),
                _ => throw new Exception($"Could not load game library {gameLibrary}"),
            };
        }

        public bool IsEnabled()
        {
            var enabledGameLibraries = (GameLibrary)Settings.Instance.EnabledGameLibraries;
            return enabledGameLibraries.HasFlag(GameLibrary);
        }

        public void Disable()
        {
            var enabledGameLibraries = Settings.Instance.EnabledGameLibraries;
            enabledGameLibraries &= ~(uint)GameLibrary; // ClearFlag 
            Settings.Instance.EnabledGameLibraries = enabledGameLibraries;
        }

        public void Enable()
        {
            var enabledGameLibraries = Settings.Instance.EnabledGameLibraries;
            enabledGameLibraries |= (uint)GameLibrary; // SetFlag
            Settings.Instance.EnabledGameLibraries = enabledGameLibraries;
        }
    }
}
