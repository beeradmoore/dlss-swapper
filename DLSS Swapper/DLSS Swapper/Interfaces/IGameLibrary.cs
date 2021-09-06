using DLSS_Swapper.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Interfaces
{
    public interface IGameLibrary
    {
        string Name { get; }
        List<Game> LoadedGames { get; }
        List<Game> LoadedDLSSGames { get; }

        Task<List<Game>> ListGamesAsync();
        bool IsInstalled();
    }
}
