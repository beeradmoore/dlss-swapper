using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DLSS_Swapper.Data;
using Humanizer;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class GameControl : FakeContentDialog
    {
        public GameControl(Game game)
        {
            this.InitializeComponent();


            // This only works if the grid background has focus.
            /*
            KeyUp += (object sender, KeyRoutedEventArgs e) => {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    if (DataContext is GameControlModel gameControlModel && gameControlModel.CloseCommand.CanExecute(null))
                    {
                        gameControlModel.CloseCommand.Execute(null);
                    }
                }
            };
            */

            Resources["ContentDialogMinWidth"] = 700;
            
            DataContext = new GameControlModel(this, game);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var dialogSpace = this.GetTemplateChild("DialogSpace") as Grid;

            var leftButtons = new ContentControl()
            {
                Template = Resources["LeftButtonsControlTemplate"] as ControlTemplate,
            };
            leftButtons.DataContext = DataContext;
            Grid.SetRow(leftButtons, 1);
            dialogSpace.Children.Add(leftButtons);


            var rightButtons = new ContentControl()
            {
                Template = Resources["RightButtonsControlTemplate"] as ControlTemplate,
            };
            rightButtons.DataContext = DataContext;
            Grid.SetRow(rightButtons, 1);
            dialogSpace.Children.Add(rightButtons);
        }


        string[] customCoverValidFileTypes = new string[]
        {
                ".png",
                ".jpg",
                ".jpeg",
                ".webp",
                ".bmp",
        };

        DataPackageOperation coverDragDropAcceptedOperation = DataPackageOperation.None;
        string coverDragDropDragUIOverrideCaption = String.Empty;

        async void CoverButton_DragEnter(object sender, DragEventArgs e)
        {

            // This thing likes to break so I took the advice from this thread https://github.com/microsoft/microsoft-ui-xaml/issues/8108

            // Default to this.           
            coverDragDropAcceptedOperation = DataPackageOperation.None;
            coverDragDropDragUIOverrideCaption = String.Empty;

            e.AcceptedOperation = coverDragDropAcceptedOperation;
            e.DragUIOverride.Caption = coverDragDropDragUIOverrideCaption;

            // This await messes things up. So what we do is also handle in CoverButton_DragOver which will have hopefully
            // mean this code is finished by then. 
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 1)
            {
                var storageFile = items[0] as StorageFile;

                if (customCoverValidFileTypes.Contains(storageFile.FileType.ToLower()) == true)
                {
                    coverDragDropAcceptedOperation = DataPackageOperation.Copy;
                    coverDragDropDragUIOverrideCaption = "Add custom cover";
                }
                else
                {
                    coverDragDropAcceptedOperation = DataPackageOperation.None;
                    coverDragDropDragUIOverrideCaption = $"\"{storageFile.FileType}\" is an invalid file type";
                }
            }
            else
            {
                coverDragDropAcceptedOperation = DataPackageOperation.None;
                coverDragDropDragUIOverrideCaption = "You may only drag over a single file for a cover";
            }
        }


        void CoverButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = coverDragDropAcceptedOperation;
            e.DragUIOverride.Caption = coverDragDropDragUIOverrideCaption;
        }


        async void CoverButton_Drop(object sender, DragEventArgs e)
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 1)
            {
                var storageFile = items[0] as StorageFile;

                if (customCoverValidFileTypes.Contains(storageFile.FileType.ToLower()) == true)
                {
                    using (var stream = await storageFile.OpenStreamForReadAsync())
                    {
                        if (DataContext is GameControlModel gameControlModel)
                        {
                            gameControlModel.Game.AddCustomCover(stream);
                        }
                    }
                }
                else
                {
                    Logger.Error($"\"{storageFile.FileType}\" is an invalid file type");
                }
            }
            else
            {
                Logger.Error("You may only drag over a single cover");
            }
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Debugger.Break();
        }
    }
}
