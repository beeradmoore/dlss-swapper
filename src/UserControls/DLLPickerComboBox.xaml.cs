using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls;

public sealed partial class DLLPickerComboBox : UserControl
{
    public DLLPickerComboBoxModel ViewModel { get; private set; }

    public Game Game
    {
        get => ViewModel.Game;
        set => ViewModel.Game = value;
    }

    public GameAssetType GameAssetType
    {
        get => ViewModel.GameAssetType;
        set => ViewModel.GameAssetType = value;
    }

    public ICommand SelectionChangedCommand
    {
        get; set;
    }

    public DLLPickerComboBox()
    {
        this.InitializeComponent();
        ViewModel = new DLLPickerComboBoxModel();
        DataContext = ViewModel;
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectionChangedCommand != null)
        {
            SelectionChangedCommand.Execute(e.AddedItems[0]);
        }
    }
}
