using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class GameFilterControl : UserControl //, INotifyPropertyChanged
    {
        // So this was confusing. I don't know why the binding does not work.
        /*        
        bool _hideNonDLSSGames;
        public bool HideNonDLSSGames
        {
            get
            {
                return _hideNonDLSSGames;
            }
            set
            {
                if (_hideNonDLSSGames != value)
                {
                    _hideNonDLSSGames = value;
                    NotifyPropertyChanged();
                }
            }
        }


        bool _groupGameLibrariesTogether;
        public bool GroupGameLibrariesTogether
        {
            get
            {
                return _groupGameLibrariesTogether;
            }
            set
            {
                if (_groupGameLibrariesTogether != value)
                {
                    _groupGameLibrariesTogether = value;
                    NotifyPropertyChanged();
                }
            }
        }
        */

        public GameFilterControl()
        {
            this.InitializeComponent();

            //HideNonDLSSGames = Settings.Instance.HideNonDLSSGames;
            //GroupGameLibrariesTogether = Settings.Instance.GroupGameLibrariesTogether;
            //this.DataContext = this;

            HideNonDLSSGamesCheckBox.IsChecked = Settings.Instance.HideNonDLSSGames;
            GroupGameLibrariesTogetherCheckBox.IsChecked = Settings.Instance.GroupGameLibrariesTogether;

        }

        public bool IsHideNonDLSSGamesChecked()
        {
            return HideNonDLSSGamesCheckBox.IsChecked ?? false;
        }

        public bool IsGroupGameLibrariesTogetherChecked()
        {
            return GroupGameLibrariesTogetherCheckBox.IsChecked ?? false;
        }

        /*
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged null;
        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        */
    }
}
