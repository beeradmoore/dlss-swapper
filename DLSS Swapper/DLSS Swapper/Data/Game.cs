using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    public class Game : IComparable<Game>, INotifyPropertyChanged
    {
        public string Title { get; set; }

        public string InstallPath { get; set; }

        public string HeaderImage { get; set; }

        string _baseDLSSVersion;
        public string BaseDLSSVersion
        {
            get { return _baseDLSSVersion; }
            set
            {
                if (_baseDLSSVersion != value)
                {
                    _baseDLSSVersion = value;
                    NotifyPropertyChanged();
                }
            }
        }

        string _currentDLSSVersion;
        public string CurrentDLSSVersion
        {
            get { return _currentDLSSVersion; }
            set
            {
                if (_currentDLSSVersion != value)
                {
                    _currentDLSSVersion = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool HasDLSS { get; set; }

        public void DetectDLSS()
        {
            BaseDLSSVersion = String.Empty;
            CurrentDLSSVersion = "N/A";
            var dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", SearchOption.AllDirectories);
            if (dlssDlls.Length > 0)
            {
                HasDLSS = true;

                // TODO: Handle a single folder with various versions of DLSS detected.
                // Currently we are just using the first.

                foreach (var dlssDll in dlssDlls)
                {
                    var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                    CurrentDLSSVersion = dllVersionInfo.FileVersion.Replace(",", ".");
                    break;
                }

                dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", SearchOption.AllDirectories);
                if (dlssDlls.Length > 0)
                {
                    foreach (var dlssDll in dlssDlls)
                    {
                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                        BaseDLSSVersion = dllVersionInfo.FileVersion.Replace(",", ".");
                        break;
                    }
                }
            }
            else
            {
                HasDLSS = false;
            }
        }

        internal bool ResetDll()
        {
            var foundDllBackups = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", SearchOption.AllDirectories);
            if (foundDllBackups.Length == 0)
            {
                return false;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(foundDllBackups.First());
            var resetToVersion = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";

            foreach (var dll in foundDllBackups)
            {
                try
                {
                    var dllPath = Path.GetDirectoryName(dll);
                    var targetDllPath = Path.Combine(dllPath, "nvngx_dlss.dll");
                    File.Move(dll, targetDllPath, true);
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine($"ResetDll Error: {err.Message}");
                    return false;
                }
            }

            CurrentDLSSVersion = resetToVersion;
            BaseDLSSVersion = String.Empty;

            return true;
        }

        internal bool UpdateDll(LocalDll localDll)
        {
            if (localDll == null)
            {
                return false;
            }

            var foundDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", SearchOption.AllDirectories);
            if (foundDlls.Length == 0)
            {
                return false;
            }
            
            var versionInfo = FileVersionInfo.GetVersionInfo(localDll.Filename);
            var targetDllVersion = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";

            var baseDllVersion = String.Empty;

            // Backup old dlls.
            foreach (var dll in foundDlls)
            {
                var dllPath = Path.GetDirectoryName(dll);
                var targetDllPath = Path.Combine(dllPath, "nvngx_dlss.dll.dlsss");
                if (File.Exists(targetDllPath) == false)
                {
                    try
                    {
                        var defaultVersionInfo = FileVersionInfo.GetVersionInfo(dll);
                        baseDllVersion = $"{defaultVersionInfo.FileMajorPart}.{defaultVersionInfo.FileMinorPart}.{defaultVersionInfo.FileBuildPart}.{defaultVersionInfo.FilePrivatePart}";

                        File.Copy(dll, targetDllPath, true);
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateDll Error: {err.Message}");
                        return false;
                    }
                }
            }

            foreach (var dll in foundDlls)
            {
                try
                {
                    File.Copy(localDll.Filename, dll, true);
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateDll Error: {err.Message}");
                    return false;
                }
            }

            CurrentDLSSVersion = targetDllVersion;
            if (String.IsNullOrEmpty(baseDllVersion) == false)
            {
                BaseDLSSVersion = baseDllVersion;
            }
            return true;
        }

        #region IComparable<Game>
        public int CompareTo(Game other)
        {
            return Title?.CompareTo(other.Title) ?? -1;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
