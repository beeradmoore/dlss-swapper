using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls
{
    public sealed partial class GameFilterControl : UserControl //, INotifyPropertyChanged
    {
        private GameFilterControlViewModel ViewModel { get; }
        public GameFilterControl()
        {
            this.InitializeComponent();
            ViewModel = new GameFilterControlViewModel();
            //HideNonDLSSGames = Settings.Instance.HideNonDLSSGames;
            //GroupGameLibrariesTogether = Settings.Instance.GroupGameLibrariesTogether;
            //this.DataContext = this;

            HideNonDLSSGamesCheckBox.IsChecked = Settings.Instance.HideNonDLSSGames;
            GroupGameLibrariesTogetherCheckBox.IsChecked = Settings.Instance.GroupGameLibrariesTogether;

        }

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
