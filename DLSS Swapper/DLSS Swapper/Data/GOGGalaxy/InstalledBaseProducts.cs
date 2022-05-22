using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class InstalledBaseProducts
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
        public string installationDate { get; set; }
    }
}
