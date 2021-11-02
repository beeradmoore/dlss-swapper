using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    public class LocalRecord : IEquatable<LocalRecord>, INotifyPropertyChanged
    {
        public string ExpectedPath { get; private set; }

        bool _isDownloaded = false;
        public bool IsDownloaded
        {
            get { return _isDownloaded; }
            set
            {
                if (_isDownloaded != value)
                {
                    _isDownloaded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _isDownloading = false;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set
            {
                if (_isDownloading != value)
                {
                    _isDownloading = value;
                    NotifyPropertyChanged();
                }
            }
        }


        int _downloadProgress = 0;
        public int DownloadProgress
        {
            get { return _downloadProgress; }
            set
            {
                if (_downloadProgress != value)
                {
                    _downloadProgress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _hasDownloadError = false;
        public bool HasDownloadError
        {
            get { return _hasDownloadError; }
            set
            {
                if (_hasDownloadError != value)
                {
                    _hasDownloadError = value;
                    NotifyPropertyChanged();
                }
            }
        }

        string _downloadErrorMessage = String.Empty;
        public string DownloadErrorMessage
        {
            get { return _downloadErrorMessage; }
            set
            {
                if (_downloadErrorMessage != value)
                {
                    _downloadErrorMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _isImported = false;
        public bool IsImported
        {
            get { return _isImported; }
            set
            {
                if (_isDownloading != value)
                {
                    _isImported = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private LocalRecord()
        {

        }

        public static LocalRecord FromExpectedPath(string expectedPath)
        {
            var localRecord = new LocalRecord()
            {
                ExpectedPath = expectedPath,
            };

            var fullExpectedPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, expectedPath);
            if (File.Exists(fullExpectedPath))
            {
                localRecord.IsDownloaded = true;
            }

            return localRecord;
        }

        /*
        // Disabled because the non-async method seems faster. 
        public static async Task<LocalRecord> FromExpectedPathAsync(string expectedPath)
        {
            var localRecord = new LocalRecord()
            {
                ExpectedPath = expectedPath,
            };

            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            System.Diagnostics.Debug.WriteLine($"StorageFolder: {storageFolder.Path}");
            try
            {
                var dlssFile = await storageFolder.GetFileAsync(expectedPath);
                localRecord.IsDownloaded = true;
            }
            catch (Exception)
            {
                // If we couldn't load the file we assume it doesn't exist locally
                localRecord.IsDownloaded = false;
            }

            return localRecord;
        }
        */

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        internal bool Delete()
        {
            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var dlssPath = Path.GetDirectoryName(Path.Combine(storageFolder.Path, ExpectedPath));
                Directory.Delete(dlssPath, true);

                IsDownloaded = false;
                IsDownloading = false;
                DownloadProgress = 0;
                HasDownloadError = false;
                DownloadErrorMessage = String.Empty;
                // We don't update IsImported here as that wont change.
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /*
        internal async Task<bool> DeleteAsync()
        {
            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var dlssFile = await storageFolder.GetFileAsync(ExpectedPath);
                await dlssFile.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
                
                IsDownloaded = false;
                IsDownloading = false;
                DownloadProgress = 0;
                HasDownloadError = false;
                DownloadErrorMessage = String.Empty;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        */

        internal void UpdateFromNewLocalRecord(LocalRecord localRecord)
        {
            // First make sure expected path matches on both.
            if (Equals(localRecord) == false)
            {
                return;
            }

            ExpectedPath = localRecord.ExpectedPath;
            IsDownloaded = localRecord.IsDownloaded;
            IsDownloading = localRecord.IsDownloading;
            DownloadProgress = localRecord.DownloadProgress;
            HasDownloadError = localRecord.HasDownloadError;
            DownloadErrorMessage = localRecord.DownloadErrorMessage;
            IsImported = localRecord.IsImported;
        }

        public bool Equals(LocalRecord other)
        {
            // Make sure other and other.ExpectedPath are not null or empty. This also means
            // that this.ExpectedPath can't be null or empty and have this return true.
            if (String.IsNullOrWhiteSpace(other?.ExpectedPath))
            {
                return false;
            }

            return String.Equals(ExpectedPath, other.ExpectedPath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
