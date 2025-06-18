using System;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryPage : Page
    {
        public static string PageTag { get; } = "PageTag_Library";

        public LibraryPageModel ViewModel { get; private set; }

        public LibraryPage()
        {
            this.InitializeComponent();
            ViewModel = new LibraryPageModel(this);
        }


        void MainGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // via: https://stackoverflow.com/a/41141249
            var columns = Math.Ceiling(MainGridView.ActualWidth / 400);
            ((ItemsWrapGrid)MainGridView.ItemsPanelRoot).ItemWidth = (e.NewSize.Width / columns) - 1;
        }

        private void MainGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DLLRecord dllRecord)
            {
                var dialog = new EasyContentDialog(XamlRoot)
                {
                    Title = DLLManager.Instance.GetAssetTypeName(dllRecord.AssetType),
                    CloseButtonText = ResourceHelper.GetString("Cancel"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = new DLLRecordInfoControl(dllRecord),
                };
                _ = dialog.ShowAsync();
            }
        }
    }
}
