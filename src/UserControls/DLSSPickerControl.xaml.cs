using System.Collections.Generic;
using System.Linq;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class DLSSPickerControl : UserControl
    {
        private readonly Game _game;
        public List<DLSSRecord> DLSSRecords { get; } = [];

        public DLSSPickerControl(Game game)
        {
            _game = game;
            bool hideNotDownloaded = Settings.Instance.HideNotDownloadedVersions;
            if (hideNotDownloaded)
            {
                DLSSRecords.AddRange(App.CurrentApp.MainWindow.CurrentDLSSRecords.Where(r => r.LocalRecord.IsDownloaded is true));
            }
            else
            {
                DLSSRecords.AddRange(App.CurrentApp.MainWindow.CurrentDLSSRecords);
            }

            this.InitializeComponent();
            DataContext = this;

            var detectedVersion = DLSSRecords.FirstOrDefault(v => v.MD5Hash == game.CurrentDLSSHash);
            if (detectedVersion is not null)
            {
                DLSSRecordsListView.SelectedItem = detectedVersion;
            }
            // TODO: If you select an imported DLSS
            // else { }
        }

        internal DLSSRecord GetSelectedDLSSRecord()
        {
            return DLSSRecordsListView.SelectedItem as DLSSRecord;
        }
    }
}
