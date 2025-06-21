using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.UserControls
{
    public sealed partial class GameControl : FakeContentDialog
    {
        public GameControlModel ViewModel { get; private set; }

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

            ViewModel = new GameControlModel(this, game);
            DataContext = ViewModel;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var dialogSpace = this.GetTemplateChild("DialogSpace") as Grid;

            if (dialogSpace is not null)
            {
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
        string coverDragDropDragUIOverrideCaption = string.Empty;

        async void CoverButton_DragEnter(object sender, DragEventArgs e)
        {

            // This thing likes to break so I took the advice from this thread https://github.com/microsoft/microsoft-ui-xaml/issues/8108

            // Default to this.
            coverDragDropAcceptedOperation = DataPackageOperation.None;
            coverDragDropDragUIOverrideCaption = string.Empty;

            e.AcceptedOperation = coverDragDropAcceptedOperation;
            e.DragUIOverride.Caption = coverDragDropDragUIOverrideCaption;

            // This await messes things up. So what we do is also handle in CoverButton_DragOver which will have hopefully
            // mean this code is finished by then.
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 1)
            {
                var storageFile = items[0] as StorageFile;

                if (storageFile is null)
                {
                    coverDragDropAcceptedOperation = DataPackageOperation.None;
                    coverDragDropDragUIOverrideCaption = ResourceHelper.GetString("GamePage_StorageFileIsNull");
                }
                else if (customCoverValidFileTypes.Contains(storageFile.FileType.ToLower()) == true)
                {
                    coverDragDropAcceptedOperation = DataPackageOperation.Copy;
                    coverDragDropDragUIOverrideCaption = ResourceHelper.GetString("GamePage_AddCustomCover");
                }
                else
                {
                    coverDragDropAcceptedOperation = DataPackageOperation.None;
                    coverDragDropDragUIOverrideCaption = ResourceHelper.GetFormattedResourceTemplate("GamePage_InvalidFileTypeTemplate", storageFile.FileType);
                }
            }
            else
            {
                coverDragDropAcceptedOperation = DataPackageOperation.None;
                coverDragDropDragUIOverrideCaption = ResourceHelper.GetString("GamePage_YouMayOnlyDragOneFileCover");
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
                if (storageFile is null)
                {
                    Logger.Error("storageFile is null");
                }
                else if (customCoverValidFileTypes.Contains(storageFile.FileType.ToLower()) == true)
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
