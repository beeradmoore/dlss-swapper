using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
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
    public sealed partial class GameControl : UserControl
    {
        public GameControl(Game game)
        {
            this.InitializeComponent();

            DataContext = new GameControlModel(this, game);
        }

        public void ShowAsync()
        {
            if (App.CurrentApp.MainWindow.Content is Grid rootGrid)
            {
                var mainNavigationView = rootGrid.FindChild<NavigationView>(x => x.Name == "MainNavigationView");
                var appTitleBar = rootGrid.FindChild<Grid>(x => x.Name == "AppTitleBar");


                Grid.SetColumnSpan(this, rootGrid.ColumnDefinitions.Count);
                Grid.SetRowSpan(this, rootGrid.RowDefinitions.Count);
               // mainNavigationView.IsEnabled = false;
               // GridContent.Margin = new Thickness(20, 20 + appTitleBar.ActualHeight, 20, 20);

                rootGrid.Children.Add(this);


                //colorStoryboard.Begin();

                /*
                var storyboard = new Storyboard();
                var colorAnimation = new ColorAnimation();
                colorAnimation.From = Colors.Transparent;
                colorAnimation.To = Colors.Black;
                colorAnimation.Duration = new Duration(TimeSpan.FromSeconds(4));

                Storyboard.SetTarget(colorAnimation, this);
                Storyboard.SetTargetProperty(colorAnimation, "(Background).(SolidColorBrush.Color)");

                storyboard.Children.Add(colorAnimation);

                storyboard.Begin();
                */


            }
            /*

            //<UserControl.Resources>
        < Storyboard x: Name = "colorStoryboard" >

            < !--Animate the background color of the canvas from red to green
        over 4 seconds. -- >
            < ColorAnimation Storyboard.TargetName = "myStackPanel" Storyboard.TargetProperty = "(Panel.Background).(SolidColorBrush.Color)" From = "Transparent" To = "Black" Duration = "0:0:4" />

        </ Storyboard >
    </ UserControl.Resources >
            colorStoryboard.Begin();
            */
        }

        public void Hide()
        {
            if (App.CurrentApp.MainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Remove(this);
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
    }
}
