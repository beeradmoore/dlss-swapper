using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
        public List<LocalDll> LocalDlls { get; } = new List<LocalDll>();

        public DLSSPickerControl(Game game, List<LocalDll> localDlls)
        {
            _game = game;
            LocalDlls.AddRange(localDlls);

            this.InitializeComponent();
            DataContext = this;

            var detectedVersion = LocalDlls.FirstOrDefault(v => v.Version == game.CurrentDLSSVersion);
            if (detectedVersion != null)
            {
                VersionComboBox.SelectedItem = detectedVersion;
            }
        }

        internal LocalDll GetSelectedLocalDll()
        {
            return (VersionComboBox.SelectedItem as LocalDll);
        }
    }
}
