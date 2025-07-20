using System;
using System.Collections.Generic;
using Windows.Win32.UI.Shell;
using Windows.Win32.System.Com;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.Diagnostics;
using System.IO;

namespace DLSS_Swapper.Helpers;

internal class FileSystemHelper
{
    const int ERROR_CANCELLED = unchecked((int)0x800704c7);

    internal static string OpenFolder(nint hWnd, string? defaultPath = null, string? okButtonLabel = null)
    {
        try
        {
            var hResult = PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var folderOpenDialog);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            // Set options to pick a folder
            folderOpenDialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);

            // If no default is provided (or doesn't exist) use My Computer.
            // I hope users don't just click C:\ and call it a day -_-
            if (string.IsNullOrWhiteSpace(defaultPath) || Directory.Exists(defaultPath) == false)
            { 
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            }

            // Set the default folder.
            hResult = PInvoke.SHCreateItemFromParsingName(defaultPath, null, typeof(IShellItem).GUID, out var directoryShellItem);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            folderOpenDialog.SetFolder((IShellItem)directoryShellItem);
            folderOpenDialog.SetDefaultFolder((IShellItem)directoryShellItem);
            if (string.IsNullOrWhiteSpace(okButtonLabel) == false)
            {
                folderOpenDialog.SetOkButtonLabel(okButtonLabel);
            }
            folderOpenDialog.Show(new HWND(hWnd));

            folderOpenDialog.GetResult(out var ppsi);

            unsafe
            {
                PWSTR filename;
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filename);
                var pathName = filename.ToString();
                return pathName;
            }
        }
        catch (COMException ex) when (ex.HResult == ERROR_CANCELLED)
        {
            // NOOP
            return string.Empty;
        }
    }

    internal static string OpenFile(nint hWnd, string filter = "All files|*.*", string? defaultPath = null, string? defaultFileName = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        try
        {
            var hResult = PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileOpenDialog);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            var extensions = new List<COMDLG_FILTERSPEC>();

            if (string.IsNullOrWhiteSpace(filter) == false)
            {
                var tokens = filter.Split('|');
                if (0 == tokens.Length % 2)
                {
                    // All even numbered tokens should be labels.
                    // Odd numbered tokens are the associated extensions.
                    for (var i = 1; i < tokens.Length; i += 2)
                    {
                        unsafe
                        {
                            COMDLG_FILTERSPEC extension;

                            extension.pszSpec = (char*)Marshal.StringToHGlobalUni(tokens[i]);
                            extension.pszName = (char*)Marshal.StringToHGlobalUni(tokens[i - 1]);
                            extensions.Add(extension);
                        }
                    }
                }
            }

            fileOpenDialog.SetFileTypes(extensions.ToArray());


            // If no default is provided (or doesn't exist) use My Computer.
            // I hope users don't just click C:\ and call it a day -_-
            if (string.IsNullOrWhiteSpace(defaultPath) || Directory.Exists(defaultPath) == false)
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            }


            // Set the default folder.
            hResult = PInvoke.SHCreateItemFromParsingName(defaultPath, null, typeof(IShellItem).GUID, out var directoryShellItem);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            fileOpenDialog.SetFolder((IShellItem)directoryShellItem);
            fileOpenDialog.SetDefaultFolder((IShellItem)directoryShellItem);

            if (string.IsNullOrWhiteSpace(okButtonLabel) == false)
            {
                fileOpenDialog.SetOkButtonLabel(okButtonLabel);
            }

            if (string.IsNullOrWhiteSpace(defaultFileName) == false)
            {
                fileOpenDialog.SetFileName(defaultFileName);
            }

            if (string.IsNullOrWhiteSpace(defaultExtension) == false)
            {
                fileOpenDialog.SetDefaultExtension(defaultExtension);
            }

            fileOpenDialog.Show(new HWND(hWnd));

            fileOpenDialog.GetResult(out var ppsi);

            unsafe
            {
                PWSTR filename;
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filename);
                var pathName = filename.ToString();
                return pathName;
            }
        }
        catch (COMException ex) when (ex.HResult == ERROR_CANCELLED)
        {
            // NOOP
            return string.Empty;
        }
    }


    internal static string SaveFile(nint hWnd, string filter = "All files|*.*", string? defaultPath = null, string? defaultFileName = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        try
        {
            var hResult = PInvoke.CoCreateInstance<IFileSaveDialog>(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileOpenDialog);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            var extensions = new List<COMDLG_FILTERSPEC>();

            if (string.IsNullOrWhiteSpace(filter) == false)
            {
                var tokens = filter.Split('|');
                if (0 == tokens.Length % 2)
                {
                    // All even numbered tokens should be labels.
                    // Odd numbered tokens are the associated extensions.
                    for (var i = 1; i < tokens.Length; i += 2)
                    {
                        unsafe
                        {
                            COMDLG_FILTERSPEC extension;

                            extension.pszSpec = (char*)Marshal.StringToHGlobalUni(tokens[i]);
                            extension.pszName = (char*)Marshal.StringToHGlobalUni(tokens[i - 1]);
                            extensions.Add(extension);
                        }
                    }
                }
            }

            fileOpenDialog.SetFileTypes(extensions.ToArray());


            // If no default is provided (or doesn't exist) use My Computer.
            // I hope users don't just click C:\ and call it a day -_-
            if (string.IsNullOrWhiteSpace(defaultPath) || Directory.Exists(defaultPath) == false)
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            }


            // Set the default folder.
            hResult = PInvoke.SHCreateItemFromParsingName(defaultPath, null, typeof(IShellItem).GUID, out var directoryShellItem);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            fileOpenDialog.SetFolder((IShellItem)directoryShellItem);
            fileOpenDialog.SetDefaultFolder((IShellItem)directoryShellItem);

            if (string.IsNullOrWhiteSpace(okButtonLabel) == false)
            {
                fileOpenDialog.SetOkButtonLabel(okButtonLabel);
            }

            if (string.IsNullOrWhiteSpace(defaultFileName) == false)
            {
                fileOpenDialog.SetFileName(defaultFileName);
            }

            if (string.IsNullOrWhiteSpace(defaultExtension) == false)
            {
                fileOpenDialog.SetDefaultExtension(defaultExtension);
            }

            fileOpenDialog.Show(new HWND(hWnd));

            fileOpenDialog.GetResult(out var ppsi);

            unsafe
            {
                PWSTR filename;
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filename);
                var pathName = filename.ToString();
                return pathName;
            }
        }
        catch (COMException ex) when (ex.HResult == ERROR_CANCELLED)
        {
            // NOOP
            return string.Empty;
        }
    }
}
