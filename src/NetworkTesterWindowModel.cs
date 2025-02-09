using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class NetworkTesterWindowModel : ObservableObject
{
    WeakReference<NetworkTesterWindow> _weakWindow;
    readonly string _dlssSwapperDownloadTestLink = "https://dlss-swapper-downloads.beeradmoore.com/dlss/nvngx_dlss_v1.0.0.0.zip";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunningTest))]
    [NotifyPropertyChangedFor(nameof(IsNotRunningTest))]
    public partial bool RunningTest1 { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunningTest))]
    [NotifyPropertyChangedFor(nameof(IsNotRunningTest))]
    public partial bool RunningTest2 { get; set; } = false;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunningTest))]
    [NotifyPropertyChangedFor(nameof(IsNotRunningTest))]
    public partial bool RunningTest3 { get; set; } = false;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunningTest))]
    [NotifyPropertyChangedFor(nameof(IsNotRunningTest))]
    public partial bool RunningTest4 { get; set; } = false;

    public bool IsRunningTest => RunningTest1 || RunningTest2 || RunningTest3 || RunningTest4;
    public bool IsNotRunningTest => IsRunningTest == false;

    [ObservableProperty]
    public partial string TestResults { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Test1Result { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Test2Result { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Test3Result { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Test4Result { get; set; } = string.Empty;

    public NetworkTesterWindowModel(NetworkTesterWindow window)
    {
        _weakWindow = new WeakReference<NetworkTesterWindow>(window);

        TestResults += $"DLSS Swapper version: v{App.CurrentApp.GetVersionString()}\n\n";
    }

    [RelayCommand]
    async Task RunTest1Async()
    {
        RunningTest1 = true;
        Test1Result = string.Empty;
        var testStart = DateTime.Now;
        TestResults += $"Test 1: Accessing google.com\n";

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var response = await App.CurrentApp.HttpClient.GetAsync("https://google.com", System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                {
                    TestResults += $"Test 1: Status code - {response.StatusCode}\n";

                    response.EnsureSuccessStatusCode();

                    await response.Content.CopyToAsync(memoryStream);
                }

                TestResults += $"Test 1: Downloaded {memoryStream.Length} bytes\n";
            }
            Test1Result = "✅";
        }
        catch (Exception err)
        {
            Test1Result = "❌";
            TestResults += $"Test 1 failed: {err.Message}\n";
            RunningTest1 = false;
        }
        finally
        {
            var duration = (DateTime.Now - testStart).TotalSeconds;
            TestResults += $"Test 1: Duration {duration:0.00} seconds\n\n";
            RunningTest1 = false;
        }
    }

    [RelayCommand]
    async Task RunTest2Async()
    {
        RunningTest2 = true;
        Test2Result = string.Empty;
        var testStart = DateTime.Now;
        TestResults += $"Test 2: Accessing bing.com\n";

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var response = await App.CurrentApp.HttpClient.GetAsync("https://bing.com", System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                {
                    TestResults += $"Test 2: Status code - {response.StatusCode}\n";

                    response.EnsureSuccessStatusCode();

                    await response.Content.CopyToAsync(memoryStream);
                }

                TestResults += $"Test 2: Downloaded {memoryStream.Length} bytes\n";
            }
            Test2Result = "✅";
        }
        catch (Exception err)
        {
            Test2Result = "❌";
            TestResults += $"Test 2 failed: {err.Message}\n";
            RunningTest2 = false;
        }
        finally
        {
            var duration = (DateTime.Now - testStart).TotalSeconds;
            TestResults += $"Test 2: Duration {duration:0.00} seconds\n\n";
            RunningTest2 = false;
        }
    }

    [RelayCommand]
    async Task RunTest3Async()
    {
        RunningTest3 = true;
        Test3Result = string.Empty;
        var testStart = DateTime.Now;
        TestResults += $"Test 3: Downloading from DLSS Swapper DLL file server\n";

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var response = await App.CurrentApp.HttpClient.GetAsync(_dlssSwapperDownloadTestLink, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                {
                    TestResults += $"Test 3: Status code - {response.StatusCode}\n";

                    response.EnsureSuccessStatusCode();

                    await response.Content.CopyToAsync(memoryStream);
                }

                TestResults += $"Test 3: Downloaded {memoryStream.Length} bytes\n";
            }
            Test3Result = "✅";
        }
        catch (Exception err)
        {
            Test3Result = "❌";
            TestResults += $"Test 3 failed: {err.Message}\n";
            RunningTest3 = false;
        }
        finally
        {
            var duration = (DateTime.Now - testStart).TotalSeconds;
            TestResults += $"Test 3: Duration {duration:0.00} seconds\n\n";
            RunningTest3 = false;
        }
    }

    [RelayCommand]
    async Task RunTest4Async()
    {
        Test4Result = string.Empty;
        TestResults += $"Test 4: Downloading from DLSS Swapper DLL file server in a web browser\n";

        if (_weakWindow.TryGetTarget(out NetworkTesterWindow? networkTesterWindow) == true)
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
            };
            stackPanel.Children.Add(new TextBlock()
            {
                Text = "Click the open in browser button, or copy and paste following link into your browser. Does it download a file in your browser?",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            });
            var horizontalGrid = new Grid()
            {
                ColumnSpacing = 12,
            };
            horizontalGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            horizontalGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            horizontalGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            var linkTextBox = new TextBox()
            {
                Text = _dlssSwapperDownloadTestLink,
                IsReadOnly = true,
            };
            Grid.SetColumn(linkTextBox, 0);
            horizontalGrid.Children.Add(linkTextBox);

            var copyButton = new Button()
            {
                Content = "Copy",
            };
            copyButton.Tapped += (sender, e) =>
            {
                var package = new DataPackage();
                package.SetText(_dlssSwapperDownloadTestLink);
                Clipboard.SetContent(package);
            };
            Grid.SetColumn(copyButton, 1);

            var openButton = new Button()
            {
                Content = "Open in browser"
            };
            openButton.Tapped += async (sender, e) =>
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(_dlssSwapperDownloadTestLink));
            };
            Grid.SetColumn(openButton, 2);

            horizontalGrid.Children.Add(copyButton);
            horizontalGrid.Children.Add(openButton);

            stackPanel.Children.Add(horizontalGrid);
            var dialog = new EasyContentDialog(networkTesterWindow.Content.XamlRoot)
            {
                Title = $"Browser Test",
                PrimaryButtonText = "Works!",
                SecondaryButtonText = "Still does not work",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                Content = stackPanel,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Test4Result = "✅";
                TestResults += $"Test 4: User reported it downloaded correctly\n\n";
            }
            else if (result == ContentDialogResult.Secondary)
            {
                Test4Result = "❌";
                TestResults += $"Test 4: User reported it failed to download in their web browser\n\n";
            }
            else
            {
                TestResults += $"Test 4: User cancelled the test\n\n";
            }
        }
    }

    [RelayCommand]
    void CopyTestResults()
    {
        var package = new DataPackage();
        package.SetText(TestResults);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    async Task CreateBugReportAsync()
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues/new?template=bug_report.yml"));
    }
}
