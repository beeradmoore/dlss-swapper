using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.WinUI.UI.Controls;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Language;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
                        Title = ResourceHelper.GetString("Error"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = ResourceHelper.GetString("TranslationTools_AdminError"),
                        CloseButtonText = ResourceHelper.GetString("Okay"),
                    };
                    await dialog.ShowAsync();
                    Close();
                };
            }
        }

        ResourceHelper.TranslatorModeEnabled = true;
        Closed += (sender, args) =>
        {
            ResourceHelper.TranslatorModeEnabled = false;
        };
    }


    void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var shiftStatus = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftShift) |
                Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightShift) |
                Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
            if (sender is TextBox textBox)
            {
                if (shiftStatus.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    int selectionStart = textBox.SelectionStart;
                    int selectionLenth = textBox.SelectionLength;

                    textBox.Text = textBox.Text.Remove(selectionStart, selectionLenth);
                    textBox.Text = textBox.Text.Insert(selectionStart, Environment.NewLine);

                    textBox.SelectionStart = selectionStart + 1;
                    e.Handled = true;
                }
                else
                {
                    var currentColumn = MainDataGrid.CurrentColumn;
                    var currentRowIndex = MainDataGrid.SelectedIndex;
                    var newRowIndex = currentRowIndex + 1;
                    if (newRowIndex < ViewModel.TranslationRows.Count)
                    {
                        // TODO: Ensure we don't crahs of teh end
                        MainDataGrid.SelectedIndex = newRowIndex;
                        MainDataGrid.CurrentColumn = currentColumn;

                        MainDataGrid.ScrollIntoView(ViewModel.TranslationRows[newRowIndex], currentColumn);

                        MainDataGrid.Focus(FocusState.Programmatic);
                        MainDataGrid.BeginEdit();
                    }
                    else
                    {
                        // We are at the end, remove focus.
                        //BottomStackPanel.Focus(FocusState.Programmatic);
                    }

                    //textBox.Focus(FocusState.Unfocused);
                    //FocusManager.TryMoveFocus(FocusNavigationDirection.Next, );
                    // (FocusState.Unfocused);
                    //control.Focus(FocusState.Unfocused); // Optionally, you can use FocusManager.TryMoveFocus
                    //OtherTextBox.Focus(FocusState.Programmatic); // Move focus to another TextBox
                    //FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
                    e.Handled = true;
                }
            }
        }
    }


    /*
    private void RichEditBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var shiftStatus = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftShift) |
                Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightShift) |
                Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
            if (sender is RichEditBox richEditBox)
            {
                if (shiftStatus.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    richEditBox.TextDocument.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out var text);
                    if (text is null)
                    {
                        text = string.Empty;
                    }

                    int selectionStart = richEditBox.TextDocument.Selection.StartPosition;
                    // this can be negative numbers if text was selected right to left so we absolute it
                    int selectionLenth = Math.Abs(richEditBox.TextDocument.Selection.Length);

                    // For some reason you can select a fake space at the end
                    if (selectionLenth > 0 && selectionStart < text.Length)
                    {
                        text = text.Remove(selectionStart, selectionLenth);
                    }
                    text = text.Insert(selectionStart, "\n");
                    richEditBox.TextDocument.SetText(Microsoft.UI.Text.TextSetOptions.None, text);

                    if (selectionStart + 1 <= text.Length)
                    {
                        richEditBox.TextDocument.Selection.StartPosition = selectionStart + 1;
                    }

                    e.Handled = true;
                }
                else
                {

                    var currentColumn = MainDataGrid.CurrentColumn;
                    var currentRowIndex = MainDataGrid.SelectedIndex;
                    var newRowIndex = currentRowIndex + 1;
                    if (newRowIndex < ViewModel.TranslationRows.Count)
                    {
                        // TODO: Ensure we don't crahs of teh end
                        MainDataGrid.SelectedIndex = newRowIndex;
                        MainDataGrid.CurrentColumn = currentColumn;


                        MainDataGrid.ScrollIntoView(ViewModel.TranslationRows[newRowIndex], currentColumn);


                        // Optionally, focus the cell
                        MainDataGrid.Focus(FocusState.Programmatic);  // Focus the grid itself
                        MainDataGrid.BeginEdit();
                    }
                    else
                    {
                        // We are at the end, remove focus.
                        //BottomStackPanel.Focus(FocusState.Programmatic);
                    }
                       
                    

                    //this.Content.Focus(FocusState.Programmatic);
                    //textBox.Focus(FocusState.Unfocused);
                    //control.Focus(FocusState.Unfocused); // Optionally, you can use FocusManager.TryMoveFocus
                    //OtherTextBox.Focus(FocusState.Programmatic); // Move focus to another TextBox
                    //FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
                    e.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// When the text changes in the RichEditBox we need to update the NewTranslation property.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RichEditBox_TextChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RichEditBox richEditBox && richEditBox.DataContext is TranslationRow translationRow && e.OriginalSource is null)
        {
            richEditBox.TextDocument.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out var text);
            translationRow.NewTranslation = text;
        }
    }

    /// <summary>
    /// Used to set the initial text in the RichEditBox when it is loaded.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RichEditBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is RichEditBox richEditBox && richEditBox.DataContext is TranslationRow translationRow)
        {
            richEditBox.TextDocument.SetText(Microsoft.UI.Text.TextSetOptions.None, translationRow.SourceTranslation);
        }
    }

    private void RichEditBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender is RichEditBox richEditBox && args.NewValue is TranslationRow translationRow)
        {
            richEditBox.TextDocument.SetText(Microsoft.UI.Text.TextSetOptions.None, translationRow.SourceTranslation);
            args.Handled = true;
        }
    }
    */
}
