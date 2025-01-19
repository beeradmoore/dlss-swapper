using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class DLSSPickerControl : UserControl
    {
        Game _game;
        public List<DLLRecord> DLSSRecords { get; } = new List<DLLRecord>();

        public DLSSPickerControl(Game game)
        {
            _game = game;
            DLSSRecords.AddRange(DLLManager.Instance.DLSSRecords);

            this.InitializeComponent();
            DataContext = this;

            // TODO: If you select an imported DLSS 
            // TODO: FIX
            /*
            var detectedVersion = DLSSRecords.FirstOrDefault(v => v.MD5Hash == game.CurrentDLSSHash);
            if (detectedVersion is null)
            {

            }
            else
            {
                DLSSRecordsListView.SelectedItem = detectedVersion;
            }
            */
        }

        internal DLLRecord? GetSelectedDLSSRecord()
        {
            return DLSSRecordsListView.SelectedItem as DLLRecord;
        }
    }
}
