using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TranslationToolsWindow : Window
{
    public TranslationToolsWindowModel ViewModel { get; private set; }

    public TranslationToolsWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon("Assets\\icon.ico");

        ViewModel = new TranslationToolsWindowModel(this);

        // Kick them out early if they are running as admin so we don't have to worry about the export data.
        if (Environment.IsPrivilegedProcess)
        {
            if (this.Content is Grid grid)
            {
                grid.Loaded += async (sender, e) =>
                {
                    var dialog = new EasyContentDialog(Content.XamlRoot)
                    {
                        Title = "Error",
                        DefaultButton = ContentDialogButton.Close,
                        Content = "You can not use the translation tool while it is runnning as admin. Please restart the application.",
                        CloseButtonText = "Okay",
                    };
                    await dialog.ShowAsync();
                    Close();
                };
            }
        }

        ResourceHelper.TranslatorMode = true;
        this.Closed += (sender, args) =>
        {
            ResourceHelper.TranslatorMode = false;
        };
    }
}
