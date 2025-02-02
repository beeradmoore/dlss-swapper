using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class DLSSRecordInfoControl : UserControl
    {
        public DLLRecord DLLRecord { get; private set; }

        public DLSSRecordInfoControl(DLLRecord dllRecord)
        {
            this.InitializeComponent();
            this.DLLRecord = dllRecord;
        }
    }
}
