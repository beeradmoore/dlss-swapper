using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls;

internal partial class GameLibrarySelectorControl : UserControl
{
    public GameLibrarySelectorControlModel ViewModel { get; private set; }

    public GameLibrarySelectorControl()
    {
        this.InitializeComponent();
        ViewModel = new GameLibrarySelectorControlModel();
        DataContext = ViewModel;
    }
}
