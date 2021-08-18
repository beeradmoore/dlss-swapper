using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DLSS_Swapper
{
    public static class Settings
    {
        private static string _baseDirectory;
        public static string BaseDirectory => _baseDirectory;

        public static string TechPowerUpDownloadsDirectory => Path.Combine(BaseDirectory, "Downloads", "TechPowerUp");

        public static string DllsDirectory => Path.Combine(BaseDirectory, "dlls");

        static bool _hasShownWarning = false;
        public static bool HasShownWarning
        {
            get { return _hasShownWarning; }
            set
            {
                if (_hasShownWarning != value)
                {
                    _hasShownWarning = value;
                    ApplicationData.Current.LocalSettings.Values["HasShownWarning"] = value;
                }
            }
        }

        static Settings()
        {
            // Load BaseDirectory from settings.
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.TryGetValue("HasShownWarning", out object tempHasShownWarning))
            {
                if (tempHasShownWarning is bool hasShownWarning)
                {
                    _hasShownWarning = hasShownWarning;
                }
            }

            if (localSettings.Values.TryGetValue("BaseDirectory", out object tempObject))
            {
                if (tempObject is string baseDirectory && Directory.Exists(baseDirectory))
                {
                    _baseDirectory = baseDirectory;
                }
            }

            // If BaseDirectory is not found we should default it.
            if (String.IsNullOrWhiteSpace(_baseDirectory))
            {
                _baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DLSS Swapper");
                localSettings.Values["BaseDirectory"] = _baseDirectory;
            }
        }
        public static void InitDirectories()
        {
            // TODO: Error handling

            if (Directory.Exists(BaseDirectory) == false)
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            if (Directory.Exists(TechPowerUpDownloadsDirectory) == false)
            {
                Directory.CreateDirectory(TechPowerUpDownloadsDirectory);
            }

            if (Directory.Exists(DllsDirectory) == false)
            {
                Directory.CreateDirectory(DllsDirectory);
            }
        }
    }
}
