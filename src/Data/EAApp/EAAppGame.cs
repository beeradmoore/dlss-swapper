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
        if (string.IsNullOrWhiteSpace(DisplayIconPath))
        {
            return;
        }

        /*
        There is no hard connection between the game contentId (eg, 71715 for Bejewled 3) and the slug from loading the endpoint
        https://service-aggregation-layer.juno.ea.com/graphql?operationName=GameSearchImages&variables={"field":"RELEASE_DATE","direction":"DESC","filter":{"gameTypes":["BASE_GAME"],"prereleaseGameTypes":["OPEN_BETA"],"productLifecycleFilter":{"availabilities":["ACQUIRABLE"],"lifecycleTypes":["SELF","ORIGIN_ACCESS_BASIC","ORIGIN_ACCESS_PREMIER"]}},"offset":0,"limit":500,"locale":"en"}&extensions={"persistedQuery":{"version":1,"sha256Hash":"91fa40e48f5ea0cf9e26c3fde1f99cd661217f66de82a611d08b0e810d1eccce"}}
        We can try look in the registry at say
        HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Origin Games\71715_pc to see the value DisplayName
        We can try best-case it by looking at subkeys in HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Origin Games\ for something that
        is 71715  or starts with 71715_ to get DisplayName of Bejeweled® 3
        However this may be locale specific.
        We can try get "title" from installerdata.xml which is where we got contentId. That way we
        can ensure locale matches. EG. <localeInfo locale="en_US"><title>Bejeweled® 3</title>
        Even if we did that we don't have a hard link of the name "Bejewled 3" to get the slug "bejewed-3" from the URL
        https://service-aggregation-layer.juno.ea.com/graphql?operationName=GameSearch&variables={"field":"RELEASE_DATE","direction":"DESC","filter":{"gameTypes":["BASE_GAME"],"prereleaseGameTypes":["OPEN_BETA"],"productLifecycleFilter":{"availabilities":["ACQUIRABLE"],"lifecycleTypes":["SELF","ORIGIN_ACCESS_BASIC","ORIGIN_ACCESS_PREMIER"]}},"offset":0,"limit":500,"locale":"en"}&extensions={"persistedQuery":{"version":1,"sha256Hash":"bac31eba9ff4d69912149c10d6a981b78f22c9caffd5455d9949c51202a1cb58"}}
        That data would be

        {
            "id": "bejeweled-3",
            "title": "Bejeweled 3",
            "slug": "bejeweled-3",
            "baseGameSlug": null,
            "gameType": "BASE_GAME",
            "prereleaseGameType": null,
            "subscriptionAvailabilities": [],
            "isFreeToPlay": false,
            "playFirstTrialAvailable": false,
            "releaseDate": null,
            "__typename": "GameSearchResult"
        }

        To then match the image response 

        {
            "id": "bejeweled-3",
            "keyArtImage": {
                "path": "https://app-images.ea.com/ps9x41qn6x3c/6Jn8DFWwRUffCexjj3Rmla/9c9ff2f47649389a88b346a3f9e8437f/bejeweled-3-ce-m-keyart-1x1-en.jpg",
                "__typename": "Image"
            },
            "packArtImage": {
                "path": "https://app-images.ea.com/ps9x41qn6x3c/7f6z9PMG2cVUBgN9s73ijg/041d3d14522fe4460036ca04aeab5da6/bejeweled-3-ce-m-packart-9x16-en.jpg",
                "__typename": "Image"
            },
            "logoImage": {
                "height": 244,
                "width": 752,
                "path": "https://app-images.ea.com/ps9x41qn6x3c/7CMXspRbwA85Ei7Ry9RO57/d13b36f6be065bc877b2fa6e4e55573d/bejeweled-3-ce-m-keyart-logo-en.png",
                "__typename": "Image"
            },
            "__typename": "GameSearchResult"
        }

        // Mabye we can get do a best approximation guess later with FuzzySharp or something?
        */

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
