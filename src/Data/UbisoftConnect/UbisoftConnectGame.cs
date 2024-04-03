using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.UbisoftConnect
{
    [Table("UbisoftConnectGame")]
    internal class UbisoftConnectGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.UbisoftConnect;

        [Column("local_header_image")]
        public string LocalHeaderImage { get; set; } = String.Empty;

        [Column("remote_header_image")]
        public string RemoteHeaderImage { get; set; } = String.Empty;

        public UbisoftConnectGame()
        {

        }

        public UbisoftConnectGame(string installId)
        {
            PlatformId = installId;
            SetID();
        }

        protected override void UpdateCacheImage()
        {
            if (File.Exists(LocalHeaderImage))
            {
                ResizeCover(LocalHeaderImage);
                return;
            }

            DownloadCover(RemoteHeaderImage);
        }
    }
}
