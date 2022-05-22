using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class LimitedDetails
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

        Images _imagesData = null;
        [SQLite.Ignore]
        public Images ImagesData
        {
            get
            {
                if (_imagesData == null && String.IsNullOrEmpty(Images) == false)
                {
                    // Make sure failed deserialize doens't crash the app.
                    try
                    {
                        _imagesData = System.Text.Json.JsonSerializer.Deserialize<Images>(Images);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Error deserialzing GOG images data for {Title}");
                    }
                }

                return _imagesData;
            }
        }
    }
}
