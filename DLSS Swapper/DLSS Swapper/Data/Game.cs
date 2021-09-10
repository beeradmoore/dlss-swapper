using System;
using Microsoft.UI.Xaml;
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

        Version _baseDLSSVersion;
        public Version BaseDLSSVersion
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

        Version _currentDLSSVersion;
        public Version CurrentDLSSVersion
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

        bool _dlssChecked;
        public bool DLSSChecked
        {
            get { return _dlssChecked; }
            set
            {
                if (_dlssChecked != value)
                {
                    _dlssChecked = value;
                    NotifyPropertyChanged();
                    DLSSCheckedEvent?.Invoke(HasDLSS);
                }
            }
        }

        public delegate void DLSSCheckedDelegate(bool hasDLSS);
        public event DLSSCheckedDelegate DLSSCheckedEvent;

        internal bool ResetDll()
        {
            var foundDllBackups = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", SearchOption.AllDirectories);
            if (foundDllBackups.Length == 0)
            {
                return false;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(foundDllBackups.First());
            var resetToVersion = new Version(versionInfo.FileVersion.Replace(',', '.'));

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
            BaseDLSSVersion = null;

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
            var targetDllVersion = new Version(versionInfo.FileVersion.Replace(',', '.'));

            Version baseDllVersion = null;

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
                        baseDllVersion = new Version(defaultVersionInfo.FileVersion.Replace(',', '.'));

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
            if (baseDllVersion != null)
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
