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
    internal struct FileFilter
    {
        public string Name { get; }
        public string Spec { get; }

        public FileFilter(string name, string spec)
        {
            Name = name;
            Spec = spec;
        }
    }

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

    internal static string OpenFile(nint hWnd, IReadOnlyList<FileFilter>? filters, string? defaultPath = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        var openedFile = OpenFileInternal(hWnd, false, filters, defaultPath, defaultExtension, okButtonLabel);
        if (openedFile.Count == 1)
        {
            return openedFile[0];
        }
        return string.Empty;
    }

    internal static List<string> OpenMultipleFiles(nint hWnd, IReadOnlyList<FileFilter>? filters, string? defaultPath = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        return OpenFileInternal(hWnd, true, filters, defaultPath, defaultExtension, okButtonLabel);
    }

    static List<string> OpenFileInternal(nint hWnd, bool pickMultiple, IReadOnlyList<FileFilter>? filters, string? defaultPath = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        try
        {
            var hResult = PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileOpenDialog);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }


            if (filters is null || filters.Count == 0)
            {
                filters = new List<FileFilter>()
                {
                    new FileFilter("All files", "*.*")
                }.AsReadOnly();
            }

            var extensions = new COMDLG_FILTERSPEC[filters.Count];

            for (var i = 0; i < filters.Count; ++i)
            {
                unsafe
                {
                    COMDLG_FILTERSPEC extension;
                    extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i].Spec);
                    extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i].Name);
                    extensions[i] = extension;
                }
            }

            fileOpenDialog.SetFileTypes(extensions);


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

            // This does not seem to do anything.
            if (string.IsNullOrWhiteSpace(defaultExtension) == false)
            {
                fileOpenDialog.SetDefaultExtension(defaultExtension);
            }

            if (pickMultiple)
            {
                fileOpenDialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);
            }
            else
            {
                fileOpenDialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);
            }

            fileOpenDialog.Show(new HWND(hWnd));

            var results = new List<string>();

            if (pickMultiple)
            {
                fileOpenDialog.GetResults(out var ppsiArray);

                unsafe
                {
                    ppsiArray.GetCount(out uint count);
                    for (uint i = 0; i < count; ++i)
                    {
                        ppsiArray.GetItemAt(i, out var ppsi);

                        if (ppsi is null)
                        {
                            throw new Exception("Failed to get result from file open dialog.");
                        }

                        PWSTR filename;
                        ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filename);
                        var pathName = filename.ToString();
                        results.Add(pathName);
                    }
                }
            }
            else
            {
                fileOpenDialog.GetResult(out var ppsi);

                if (ppsi is null)
                {
                    throw new Exception("Failed to get result from file open dialog.");
                }

                unsafe
                {
                    PWSTR filename;
                    ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filename);
                    var pathName = filename.ToString();
                    results.Add(pathName);
                }
            }

            return results;
        }
        catch (COMException ex) when (ex.HResult == ERROR_CANCELLED)
        {
            // NOOP
            return new List<string>();
        }
    }


    internal static string SaveFile(nint hWnd, IReadOnlyList<FileFilter>? filters, string? defaultPath = null, string? defaultFileName = null, string? defaultExtension = null, string? okButtonLabel = null)
    {
        try
        {
            var hResult = PInvoke.CoCreateInstance<IFileSaveDialog>(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileSaveDialog);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }


            if (filters is null || filters.Count == 0)
            {
                filters = new List<FileFilter>()
                {
                    new FileFilter("All files", "*.*")
                }.AsReadOnly();
            }

            var extensions = new COMDLG_FILTERSPEC[filters.Count];

            for (var i = 0; i < filters.Count; ++i)
            {
                unsafe
                {
                    COMDLG_FILTERSPEC extension;
                    extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i].Spec);
                    extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i].Name);
                    extensions[i] = extension;
                }
            }

            fileSaveDialog.SetFileTypes(extensions);


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

            fileSaveDialog.SetFolder((IShellItem)directoryShellItem);
            fileSaveDialog.SetDefaultFolder((IShellItem)directoryShellItem);

            if (string.IsNullOrWhiteSpace(okButtonLabel) == false)
            {
                fileSaveDialog.SetOkButtonLabel(okButtonLabel);
            }

            if (string.IsNullOrWhiteSpace(defaultFileName) == false)
            {
                fileSaveDialog.SetFileName(defaultFileName);
            }


            // This does not seem to do anything. Disabled for now.
            if (string.IsNullOrWhiteSpace(defaultExtension) == false)
            {
                fileSaveDialog.SetDefaultExtension(defaultExtension);
            }

            fileSaveDialog.Show(new HWND(hWnd));

            fileSaveDialog.GetResult(out var ppsi);

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
