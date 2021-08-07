using AsyncAwaitBestPractices;
using DLSS_Swapper.Data.TechPowerUp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TechPowerUpDownloadPage : Page
    {
        // TODO: Change to our own web thing so we can display error message first if needed.
        const string BaseTechPowerUpDownloadUrl = "https://www.techpowerup.com/download/nvidia-dlss-dll/";

        public ObservableCollection<TechPowerUpLocalItem> TechPowerUpItems { get; } = new ObservableCollection<TechPowerUpLocalItem>();
        public TechPowerUpDownloadPage()
        {
            this.InitializeComponent();
            DataContext = this;
            MainWebView.Source = new Uri(BaseTechPowerUpDownloadUrl);
            GetTechPowerUpData().SafeFireAndForget();
        }

        async Task GetTechPowerUpData()
        {
            string url = "https://gist.githubusercontent.com/beeradmoore/3467646864751964dbf22f462c2e5b1e/raw/techpowerup_dlss_downloads.json";

            try
            {
                using var client = new HttpClient();
                using var stream = await client.GetStreamAsync(url);

                var items = await JsonSerializer.DeserializeAsync<List<TechPowerUpDownloadItem>>(stream);

                DispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var item in items)
                    {
                        TechPowerUpItems.Add(new TechPowerUpLocalItem(item));
                    }

                    LoadingGrid.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"GetData Error: {err.Message}");

                DispatcherQueue.TryEnqueue(async () =>
                {
                    ContentDialog dialog = new ContentDialog();
                    dialog.Title = "Error loading downloads page";
                    dialog.Content = err.Message;
                    dialog.CloseButtonText = "Okay";
                    dialog.XamlRoot = this.XamlRoot;

                    var result = await dialog.ShowAsync();
                });
            }
        }
      

        void MainWebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri.EndsWith(".zip"))
            {
                args.Cancel = true;
                MainWebView.Source = new Uri(BaseTechPowerUpDownloadUrl);

                var url = args.Uri.ToString();
                var filename = Path.GetFileName(url);
                var localItem = TechPowerUpItems.FirstOrDefault<TechPowerUpLocalItem>(x => x.DownloadItem.FileName == filename);
                if (localItem != null)
                {
                    localItem.StartDownload(url);
                }
            }
        }

        async void ExtractButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TechPowerUpLocalItem localItem)
            {
                await localItem.ExtractDll();
            }
        }
      
        void DeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TechPowerUpLocalItem localItem)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    var dialog = new ContentDialog()
                    {
                        Content = $"Delete {localItem.DownloadItem.FileName}?",
                        PrimaryButtonText = "Delete",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.XamlRoot,
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        localItem.IsDownloaded = false;
                        File.Delete(localItem.LocalFile);
                    }
                });
            }
        }
    }
}
