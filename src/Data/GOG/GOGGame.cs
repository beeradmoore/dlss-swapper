using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Xaml;

namespace DLSS_Swapper.Data.GOG
{
    internal class GOGGame : Game
    {
        public override GameLibrary GameLibrary => GameLibrary.GOG;

        public override bool IsReadyToPlay => true;

        public List<string> PotentialLocalHeaders { get; } = new List<string>();
        public string FallbackHeaderUrl { get; set; } = string.Empty;

        public GOGGame()
        {

        }

        public GOGGame(string gameId)
        {
            PlatformId = gameId;
            SetID();
        }

        protected override async Task UpdateCacheImageAsync()
        {
            foreach (var potentialLocalHeader in PotentialLocalHeaders)
            {
                if (File.Exists(potentialLocalHeader))
                {
                    using (var fileStream = File.Open(potentialLocalHeader, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await ResizeCoverAsync(fileStream).ConfigureAwait(false);
                    }
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(FallbackHeaderUrl) == false)
            {
                await DownloadCoverAsync(FallbackHeaderUrl).ConfigureAwait(false);
                return;
            }


            // If we don't have a cover to download we can try get it from various search APIs.
            // Some games are not found here (eg. Warecraft III) and instead will fallback
            // to using the direct product loading below. Unfortuantly using the product
            // endpoint does not contain boxart image urls so the images are not really
            // what we want, but at least there is images.


            try
            {
                var url = "https://catalog.gog.com/v1/catalog?order=desc:score&productType=in:game&query=like:" + Uri.EscapeDataString(Title);
                var fileDownloader = new FileDownloader(url);
                using (var memoryStream = new MemoryStream())
                {
                    await fileDownloader.DownloadFileToStreamAsync(memoryStream);
                    memoryStream.Position = 0;

                    var catalogResponse = JsonSerializer.Deserialize<GOGCatalogResponse>(memoryStream);
                    if (catalogResponse is null)
                    {
                        throw new Exception($"Could not deserialize GOGCatalogResponse for url, {url}");
                    }

                    if (catalogResponse.Products.Length == 0)
                    {
                        throw new Exception($"Could not find any GOGCatalogProduct for url, {url}");
                    }

                    foreach (var product in catalogResponse.Products)
                    {
                        if (product.Id.Equals(PlatformId, StringComparison.OrdinalIgnoreCase) == false)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(product.CoverVertical) == false)
                        {
                            await DownloadCoverAsync(product.CoverVertical).ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
                //Debugger.Break();
            }


            // If catalog failed fall back to embeded search.
            /*
            try
            {
                var url = "https://embed.gog.com/games/ajax/filtered?mediaType=game&search=" + Uri.EscapeDataString(Title);

                var fileDownloader = new FileDownloader(url);
                using (var memoryStream = new MemoryStream())
                {
                    await fileDownloader.DownloadFileToStreamAsync(memoryStream);
                    memoryStream.Position = 0;

                    var embedFilteredResponse = JsonSerializer.Deserialize(memoryStream, SourceGenerationContext.Default.GOGEmbedFilteredResponse);
                    if (embedFilteredResponse is null)
                    {
                        throw new Exception($"Could not deserialize GOGEmbedFilteredResponse for url, {url}");
                    }

                    if (embedFilteredResponse.Products.Length == 0)
                    {
                        throw new Exception($"Could not find any GOGEmbedFilteredProducts for url, {url}");
                    }

                    foreach (var product in embedFilteredResponse.Products)
                    {
                        if (product.Id.ToString().Equals(PlatformId, StringComparison.OrdinalIgnoreCase) == false)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(product.BoxImage) == false)
                        {
                            await DownloadCoverAsync($"https:{product.BoxImage}_glx_vertical_cover.webp").ConfigureAwait(false);
                            return;
                        }
                        else if (string.IsNullOrWhiteSpace(product.Image) == false)
                        {
                            await DownloadCoverAsync($"https:{product.Image}_glx_vertical_cover.webp").ConfigureAwait(false);
                            return;
                        }
                    }
                }

            }
            catch (Exception err)
            {
                Logger.Error(err);
                //Debugger.Break();
            }
            */


            // If we got here then we did not find the game in search. We can load from the product endpoint
            // But doing this the cover image is likely not what we want.
            try
            {
                var url = "https://api.gog.com/products/" + PlatformId;
                var fileDownloader = new FileDownloader(url);

                using (var memoryStream = new MemoryStream())
                {
                    await fileDownloader.DownloadFileToStreamAsync(memoryStream);

                    memoryStream.Position = 0;

                    var gogProduct = JsonSerializer.Deserialize<GOGProduct>(memoryStream);

                    if (gogProduct?.Images is not null)
                    {
                        if (string.IsNullOrWhiteSpace(gogProduct.Images.Logo) == false)
                        {
                            var newCoverUrl = $"https:{gogProduct.Images.Logo.Replace("glx_logo", "glx_vertical_cover")}";
                            await DownloadCoverAsync(newCoverUrl).ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
                Debugger.Break();
            }

        }

        public override bool UpdateFromGame(Game game)
        {
            var didChange = ParentUpdateFromGame(game);

            if (game is GOGGame gogGame)
            {
                //_localHeaderImages = xboxGame._localHeaderImages;
            }

            return didChange;
        }
    }
}
