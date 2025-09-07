using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Xbox;

[Table("XboxGame")]
public class XboxGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.XboxApp;

    public override bool IsReadyToPlay => true;

    List<string> _localHeaderImages = new List<string>();

    [Column("xbox_application_id")]
    public string ApplicationId { get; set; } = string.Empty;

    public XboxGame()
    {

    }

    public XboxGame(string familyName)
    {
        PlatformId = familyName;
        SetID();
    }

    internal void LoadApplicationId()
    {
        if (String.IsNullOrWhiteSpace(ApplicationId) == false)
        {
            // It is already loaded, don't load it again.
            return;
        }

        var appManifestFile = Path.Combine(InstallPath, "appxmanifest.xml");
        if (File.Exists(appManifestFile))
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(appManifestFile);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                string ns = doc.DocumentElement?.NamespaceURI ?? string.Empty;
                nsmgr.AddNamespace("ns", ns);

                var applicationNodes = doc.SelectNodes("//ns:Applications/ns:Application", nsmgr);
                if (applicationNodes is null)
                {
                    throw new System.Exception($"Could not find any Application nodes in {appManifestFile}");
                }

                foreach (XmlNode appNode in applicationNodes)
                {
                    var appId = appNode.Attributes?["Id"]?.Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(appId))
                    {
                        continue;
                    }

                    ApplicationId = appId;
                }
            }
            catch (Exception err)
            {
                Logger.Error(err, $"Could not load ApplicationId for {PlatformId}");
            }
        }

    }

    internal async Task SetLocalHeaderImagesAsync(List<string> localHeaderImages)
    {
        _localHeaderImages = localHeaderImages;
        await LoadCoverImageAsync();
    }

    protected override async Task UpdateCacheImageAsync()
    {
        foreach (var localHeaderImage in _localHeaderImages)
        {
            var headerImage = Path.Combine(InstallPath, localHeaderImage);
            if (File.Exists(headerImage))
            {
                using (var fileStream = File.Open(headerImage, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await ResizeCoverAsync(fileStream).ConfigureAwait(false);
                }
                return;
            }
        }
    }

    public override bool UpdateFromGame(Game game)
    {
        var didChange = ParentUpdateFromGame(game);

        return didChange;
    }
}
