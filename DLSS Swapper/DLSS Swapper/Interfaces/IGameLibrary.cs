using DLSS_Swapper.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Interfaces
{
    interface IGameLibrary
    {
        Task<List<Game>> ListGamesAsync();
        bool IsInstalled();
    }
}
