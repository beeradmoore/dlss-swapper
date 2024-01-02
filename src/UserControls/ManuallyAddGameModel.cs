using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data.CustomDirectory;
using Windows.Storage.Pickers;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : ObservableObject
    {
        ManuallyAddedGame game;

        public string GamePath { get; private set; } = String.Empty;

        [ObservableProperty]
        string headerImage = String.Empty;

        [ObservableProperty]
        string gameName = String.Empty;

        [ObservableProperty]
        string dLSSDetectedText = String.Empty;

        WeakReference<ManuallyAddGameControl> manuallyAddGameControl = null;

        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string gamePath)
        {
            this.manuallyAddGameControl = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);
            GamePath = gamePath;       
            GameName = Path.GetFileName(gamePath);

            // Temp ID
            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = GameName,
                InstallPath = GamePath,
            };
            game.ProcessGame();
            dLSSDetectedText = game.HasDLSS ? "Yes" : "No";
        }

        [RelayCommand]
        async Task AddCoverImage()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
                var fileOpenPicker = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    ViewMode = PickerViewMode.Thumbnail,
                };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".webp");
                WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

                var headerImageFile = await fileOpenPicker.PickSingleFileAsync();

                if (headerImageFile == null)
                {
                    return;
                }

                HeaderImage = headerImageFile.Path;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}
