using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SQLite;

namespace DLSS_Swapper.Data.GOG
{
    internal class InstalledBaseProduct
    {
        [Column("productId")]
        public int ProductId { get; set; }

        [Column("generation")]
        public int Generation { get; set; }

        [Column("languageId")]
        public int LanguageId { get; set; }

        [Column("installationPath")]
        public string InstallationPath { get; set; }

        [Column("installationId")]
        public long InstallationId { get; set; }

        [Column("buildId")]
        public long BuildId { get; set; }

        [Column("branch")]
        public string Branch { get; set; }

        [Column("installationDate")]
        public string InstallationDate { get; set; }
    }

    internal class LimitedDetail
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("productId")]
        public int ProductId { get; set; }

        [Column("languageId")]
        public int LanguageId { get; set; }

        [Column("is_production")]
        public bool IsProduction { get; set; }

        [Column("stored_at")]
        public long StoredAt { get; set; } // Datetime?

        [Column("title")]
        public string Title { get; set; }

        [Column("links")]
        public string Links { get; set; }

        [Column("images")]
        public string Images { get; set; }

        [Column("ProductDetailsResponseId")]
        public int ProductDetailsResponseId { get; set; }

        LimitedDetailImages _imagesData = null;
        [SQLite.Ignore]
        public LimitedDetailImages ImagesData
        {
            get
            {
                if (_imagesData == null && String.IsNullOrEmpty(Images) == false)
                {
                    // Make sure failed deserialize doens't crash the app.
                    try
                    {
                        _imagesData = JsonSerializer.Deserialize(Images, SourceGenerationContext.Default.LimitedDetailImages);
                    }
                    catch (Exception)
                    {
                        Logger.Error($"Error deserialzing GOG images data for {Title}");
                    }
                }

                return _imagesData;
            }
        }


        internal class LimitedDetailImages
        {
            [JsonPropertyName("background")]
            public string Background { get; set; }

            [JsonPropertyName("icon")]
            public string Icon { get; set; }

            [JsonPropertyName("logo")]
            public string Logo { get; set; }

            [JsonPropertyName("logo2x")]
            public string Logo2x { get; set; }

            [JsonPropertyName("menuNotificationAv")]
            public string MenuNotificationAv { get; set; }

            [JsonPropertyName("menuNotificationAv2")]
            public string MenuNotificationAv2 { get; set; }

            [JsonPropertyName("sidebarIcon")]
            public string SidebarIcon { get; set; }

            [JsonPropertyName("sidebarIcon2x")]
            public string SidebarIcon2x { get; set; }
        }
    }

    internal class WebCache
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("releaseKey")]
        public string ReleaseKey { get; set; }

        [Column("userId")]
        public long UserId { get; set; }
    }

    internal class WebCacheResource
    {
        [Column("webCacheId")]
        public int WebCacheId { get; set; }

        [Column("webCacheResourceTypeId")]
        public int WebCacheResourceTypeId { get; set; }

        [Column("filename")]
        public string Filename { get; set; }
    }

    internal class WebCacheResourceType
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        public string Type { get; set; }
    }


    internal class GamePieceType
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        public string Type { get; set; }
    }


    internal class GamePiece
    {
        [Column("releaseKey")]
        public string ReleaseKey { get; set; }

        [Column("gamePieceTypeId")]
        public int GamePieceTypeId { get; set; }

        [Column("userId")]
        public long UserId { get; set; }

        [Column("value")]
        public string Value { get; set; }

        internal class GamePieceOriginalImages
        {
            [JsonPropertyName("background")]
            public string Background { get; set; }

            [JsonPropertyName("squareIcon")]
            public string SquareIcon { get; set; }

            [JsonPropertyName("verticalCover")]
            public string VerticalCover { get; set; }
        }

        internal GamePieceOriginalImages GetValueAsOriginalImages()
        {
            return JsonSerializer.Deserialize(Value, SourceGenerationContext.Default.GamePieceOriginalImages);
        }
    }

    internal class ResourceImages
    {
        [JsonPropertyName("images\\\\background")]
        public string Background { get; set; }

        [JsonPropertyName("images\\\\logo")]
        public string Logo { get; set; }

        [JsonPropertyName("images\\\\logo2x")]
        public string Logo2x { get; set; }

        [JsonPropertyName("images\\\\icon")]
        public string Icon { get; set; }

        [JsonPropertyName("images\\\\sidebarIcon")]
        public string SidebarIcon { get; set; }

        [JsonPropertyName("images\\\\sidebarIcon2x")]
        public string SidebarIcon2x { get; set; }

        [JsonPropertyName("images\\\\menuNotificationAv")]
        public string MenuNotificationAv { get; set; }

        [JsonPropertyName("images\\\\menuNotificationAv2")]
        public string MenuNotificationAv2 { get; set; }
    }



    //originalImages


    /*
    GamePieces
    releaseKey,gamePieceTypeId,userId,value
    gog_1207658927,378,46988767830580582,"{""background"":""https:\/\/images.gog.com\/2c40bb032307ae1c58fccca9075bec260b844b868175d5c80047e5e6e6e60313_glx_bg_top_padding_7.webp?namespace=gamesdb"",""squareIcon"":""https:\/\/images.gog.com\/3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_square_icon_v2.webp?namespace=gamesdb"",""verticalCover"":""https:\/\/images.gog.com\/3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_vertical_cover.webp?namespace=gamesdb""}"


    WebCache
    id,releaseKey,userId
    141,gog_1207658927,46988767830580582


    WebCacheResources
    webCacheId,webCacheResourceTypeId,filename
    141,2,3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_square_icon_v2.webp
    141,3,3870137e89407386f33fd4c136cc3e4d09dc60c900704220f40fb72baa01b577_glx_vertical_cover.webp


    WebCacheResourceTypes
    id,type
    1,background
    2,squareIcon
    3,verticalCover
    */
}
