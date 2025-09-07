using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;
using Windows.Win32;

namespace DLSS_Swapper.Data.EAApp;


[Table("eaapp_game")]
internal class EAAppGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.EAApp;

    public override bool IsReadyToPlay => true;

    [Column("display_icon_path")]
    public string DisplayIconPath { get; set; } = string.Empty;

    public EAAppGame()
    {
    }

    public EAAppGame(string contentId)
    {
        PlatformId = contentId;
        SetID();
    }

    public override bool UpdateFromGame(Game game)
    {
        return true;
    }

    protected override async Task UpdateCacheImageAsync()
    {
        var coverUrl = EAAppLibrary.Instance.SearchForCover(this);

        if (string.IsNullOrWhiteSpace(coverUrl) == false)
        {
            var didDownload = await DownloadCoverAsync(coverUrl).ConfigureAwait(false);
            if (didDownload)
            {
                return;
            }

            Logger.Error($"Unable to download cover for {this.Title} ({coverUrl})");
        }

        // Fall back to using icon from the game exe.
        if (string.IsNullOrWhiteSpace(DisplayIconPath))
        {
            return;
        }
               
        var extension = Path.GetExtension(DisplayIconPath) ?? string.Empty;
        if (extension?.Equals(".exe", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    unsafe
                    {
                        var shinfo = new Windows.Win32.UI.Shell.SHFILEINFOW();

                        var flags = Windows.Win32.UI.Shell.SHGFI_FLAGS.SHGFI_ICON | Windows.Win32.UI.Shell.SHGFI_FLAGS.SHGFI_LARGEICON;
                        PInvoke.SHGetFileInfo(DisplayIconPath, 0, &shinfo, (uint)System.Runtime.InteropServices.Marshal.SizeOf(shinfo), flags);
                        if (shinfo.hIcon != IntPtr.Zero)
                        {
                            using (var icon = Icon.FromHandle(shinfo.hIcon))
                            {
                                if (icon is null)
                                {
                                    return;
                                }

                                using (var bitmap = icon.ToBitmap())
                                {
                                    if (bitmap is null)
                                    {
                                        return;
                                    }

                                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                                    memoryStream.Seek(0, SeekOrigin.Begin);

                                }
                            }
                            PInvoke.DestroyIcon(shinfo.hIcon);
                        }
                        else
                        {
                            Console.WriteLine("Icon not found.");
                        }
                    }

                    if (memoryStream.Length == 0)
                    {
                        Logger.Error($"Failed to extract icon from {DisplayIconPath} in game {Title}: Memory stream is empty.");
                        return;
                    }
                        
                    await ResizeCoverAsync(memoryStream).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to extract icon from {DisplayIconPath}: {ex.Message}");
                }
            }
        }
        else if (extension?.Equals(".bmp", StringComparison.InvariantCultureIgnoreCase) == true ||
                extension?.Equals(".png", StringComparison.InvariantCultureIgnoreCase) == true ||
                extension?.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) == true ||
                extension?.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) == true ||
                extension?.Equals(".webp", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            using (var fileStream = File.OpenRead(DisplayIconPath))
            {
                await ResizeCoverAsync(fileStream).ConfigureAwait(false);
            }
        }
        else
        {
            Logger.Error($"Unknown extension {extension} for DisplayIconPath {DisplayIconPath} in game {Title}");
        }
    }
}
