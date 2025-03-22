using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DLSS_Swapper.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class ImportDLLSummaryControl : UserControl
{
    ImportDLLSummaryControlModel ViewModel { get; }

    public ImportDLLSummaryControl(IReadOnlyList<DLLImportResult> dllImportResults)
    {
        this.InitializeComponent();
        ViewModel = new ImportDLLSummaryControlModel(dllImportResults);
        DataContext = ViewModel;
    }
}
