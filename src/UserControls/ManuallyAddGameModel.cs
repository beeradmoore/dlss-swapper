using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data.CustomDirectory;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Gaming.Input;
using Windows.Storage.Pickers;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : ObservableObject
    {
        ManuallyAddedGame game;

        public string InstallPath { get; private set; } = String.Empty;

        [ObservableProperty]
        string coverImage = String.Empty;

        [ObservableProperty]
        string gameName = String.Empty;

        [ObservableProperty]
        string dLSSDetectedText = String.Empty;

        WeakReference<ManuallyAddGameControl> manuallyAddGameControl = null;

        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath)
        {
            this.manuallyAddGameControl = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);
            InstallPath = installPath;       
            GameName = Path.GetFileName(installPath);

            // Temp ID
            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = GameName,
                InstallPath = InstallPath,
            };
            game.ProcessGame(false);
            dLSSDetectedText = game.HasDLSS ? "Yes" : "No";
        }

        [RelayCommand]
        async Task AddCoverImage()
        {
            // There isn't really a nice way to remove a cover as we can't prompt the user again.
            // So we are just going to let them replace it, if they want to remove it they are going to have to
            // cancel and add the game again.
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

                var coverImageFile = await fileOpenPicker.PickSingleFileAsync();

                if (coverImageFile == null)
                {
                    return;
                }

                CoverImage = coverImageFile.Path;
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }
    }
}
