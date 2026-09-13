using System.Collections.Generic;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls;

public sealed partial class BatchSwapSummaryControl : UserControl
{
    BatchSwapSummaryControlModel ViewModel { get; }

    public BatchSwapSummaryControl(IReadOnlyList<BatchSwapResult> results)
    {
        this.InitializeComponent();
        ViewModel = new BatchSwapSummaryControlModel(results);
        DataContext = ViewModel;
    }
}
