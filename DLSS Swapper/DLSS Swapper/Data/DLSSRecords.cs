using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    internal class DLSSRecords
    {
        [JsonPropertyName("stable")]
        public List<DLSSRecord> Stable { get; set; } = new List<DLSSRecord>();

        [JsonPropertyName("experimental")]
        public List<DLSSRecord> Experimental { get; set; } = new List<DLSSRecord>();

        /*
        internal DLSSRecord GetRecordFromHash(string md5Hash)
        {
            var dlssRecords = Stable.Where(x => String.Equals(x.MD5Hash, md5Hash, StringComparison.InvariantCultureIgnoreCase));
            if (dlssRecords.Any())
            {
                return dlssRecords.FirstOrDefault();
            }

            dlssRecords = Experimental.Where(x => String.Equals(x.MD5Hash, md5Hash, StringComparison.InvariantCultureIgnoreCase));
            if (dlssRecords.Any())
            {
                return dlssRecords.FirstOrDefault();
            }

            return null;
        }
        /// <summary>
        /// Check if a given DLSS dll exists based on its md5 hash
        /// </summary>
        /// <param name="md5Hash">MD5 hash of the dll</param>
        /// <returns>true if it exists, false if it does not</returns>
        internal bool DLLExists(string md5Hash, bool onlyIfDownloaded = true)
        {
            if (Stable.Any(x => String.Equals(x.MD5Hash, md5Hash, StringComparison.InvariantCultureIgnoreCase) && x.LocalRecord.IsDownloaded))
            {
                return true;
            }

            if (Experimental.Any(x => String.Equals(x.MD5Hash, md5Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }
        */
    }
}
