using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.Steam
{
    internal class SteamGame : Game
    {
        string _steamInstallPath = String.Empty;

        string _lastHeaderImage = String.Empty;
        public override string HeaderImage
        {
            get
            {
                // If we have detected this, return it other wise we figure it out.
                if (String.IsNullOrEmpty(_lastHeaderImage) == false)
                {
                    return _lastHeaderImage;
                }



                var localHeaderImagePath = Path.Combine(_steamInstallPath, "appcache", "librarycache", $"{AppId}_library_600x900.jpg");
                if (File.Exists(localHeaderImagePath))
                {
                    _lastHeaderImage = localHeaderImagePath;
                    return _lastHeaderImage;
                }


                // Fall back to web image.
                _lastHeaderImage = $"https://steamcdn-a.akamaihd.net/steam/apps/{AppId}/library_600x900_2x.jpg"; // header.jpg";

                return _lastHeaderImage;
            }
        }

        public string AppId { get; set; } = String.Empty;

        public SteamGame(string steamInstallPath)
        {
            _steamInstallPath = steamInstallPath;
        }
    }
}
