using DLSS_Swapper.Extensions;
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
    public abstract class Game : IComparable<Game>, INotifyPropertyChanged
    {
        public string Title { get; set; }

        public string InstallPath { get; set; }

        public abstract string HeaderImage { get; }

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

        string _currentDLSSHash;
        public string CurrentDLSSHash
        {
            get { return _currentDLSSHash; }
            set
            {
                if (_currentDLSSHash != value)
                {
                    _currentDLSSHash = value;
                    NotifyPropertyChanged();
                }
            }
        }

        string _baseDLSSHash;
        public string BaseDLSSHash
        {
            get { return _baseDLSSHash; }
            set
            {
                if (_baseDLSSHash != value)
                {
                    _baseDLSSHash = value;
                    NotifyPropertyChanged();
                }
            }
        }





        public bool HasDLSS { get; set; }

        public void DetectDLSS()
        {
            BaseDLSSVersion = String.Empty;
            CurrentDLSSVersion = "N/A";


            if (String.IsNullOrEmpty(InstallPath))
            {
                return;
            }

            if (Directory.Exists(InstallPath) == false)
            {
                return;
            }

            var enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
            var dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);

            if (dlssDlls.Length > 0)
            {
                HasDLSS = true;

                // TODO: Handle a single folder with various versions of DLSS detected.
                // Currently we are just using the first.

                foreach (var dlssDll in dlssDlls)
                {
                    var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                    CurrentDLSSVersion = dllVersionInfo.GetFormattedFileVersion();
                    CurrentDLSSHash = dllVersionInfo.GetMD5Hash();
                    break;
                }

                dlssDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", enumerationOptions);
                if (dlssDlls.Length > 0)
                {
                    foreach (var dlssDll in dlssDlls)
                    {
                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(dlssDll);
                        BaseDLSSVersion = dllVersionInfo.GetFormattedFileVersion();
                        BaseDLSSHash = dllVersionInfo.GetMD5Hash();
                        break;
                    }
                }
            }
            else
            {
                HasDLSS = false;
            }
        }

        internal (bool Success, string Message, bool PromptToRelaunchAsAdmin) ResetDll()
        {
            var enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
            var foundDllBackups = Directory.GetFiles(InstallPath, "nvngx_dlss.dll.dlsss", enumerationOptions);
            if (foundDllBackups.Length == 0)
            {
                return (false, "Unable to reset to default. Please repair your game manually.", false);
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
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsRunningAsAdministrator())
                    {
                        return (false, "Unable to reset to default. Please repair your game manually.", false);
                    }
                    else
                    {
                        return (false, "Unable to reset to default. Running DLSS Swapper as administrator may fix this.", true);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return (false, "Unable to reset to default. Please repair your game manually.", false);
                }
            }

            CurrentDLSSVersion = resetToVersion;
            BaseDLSSVersion = String.Empty;

            return (true, String.Empty, false);
        }

        /// <summary>
        /// Attempts to update a DLSS dll in a given game.
        /// </summary>
        /// <param name="dlssRecord"></param>
        /// <returns>Tuple containing a boolean of Success, if this is false there will be an error message in the Message response.</returns>
        internal (bool Success, string Message, bool PromptToRelaunchAsAdmin) UpdateDll(DLSSRecord dlssRecord)
        {
            if (dlssRecord == null)
            {
                return (false, "Unable to swap DLSS dll as your DLSS record was not found.", false);
            }
            var enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.AttributesToSkip |= FileAttributes.ReparsePoint;
            var foundDlls = Directory.GetFiles(InstallPath, "nvngx_dlss.dll", enumerationOptions);
            if (foundDlls.Length == 0)
            {
                return (false, "Unable to swap DLSS dll as there were no DLSS records to update.", false);
            }


            var newDllPath = Path.Combine(Storage.GetStorageFolder(), dlssRecord.LocalRecord.ExpectedPath);

            // Validate new DLL
            if (Settings.Instance.AllowUntrusted == false)
            {
                bool isTrusted = WinTrust.VerifyEmbeddedSignature(newDllPath);
                if (isTrusted == false)
                {
                    return (false, "Unable to swap DLSS dll as we are unable to verify the signature of the DLSS version you are trying to use.\nIf you wish to override this decision please enable 'Allow Untrusted' in settings.", false);
                }
            }



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
                    catch (UnauthorizedAccessException err)
                    {
                        Logger.Error($"UnauthorizedAccessException: {err.Message}");
                        if (App.CurrentApp.IsRunningAsAdministrator())
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);

                        }
                        else
                        {
                            return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err.Message);
                        return (false, "Unable to swap DLSS dll. Please check your error log for more information.", false);
                    }
                }
            }



            foreach (var dll in foundDlls)
            {
                try
                {
                    File.Copy(newDllPath, dll, true);
                }
                catch (UnauthorizedAccessException err)
                {
                    Logger.Error($"UnauthorizedAccessException: {err.Message}");
                    if (App.CurrentApp.IsRunningAsAdministrator())
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory.", false);

                    }
                    else
                    {
                        return (false, "Unable to swap DLSS dll as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                    return (false, "Unable to swap DLSS dll. Please check your error log for more information.", false);
                }
            }

            CurrentDLSSVersion = dlssRecord.Version;
            if (String.IsNullOrEmpty(baseDllVersion) == false)
            {
                BaseDLSSVersion = baseDllVersion;
            }

            return (true, String.Empty, false);
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
