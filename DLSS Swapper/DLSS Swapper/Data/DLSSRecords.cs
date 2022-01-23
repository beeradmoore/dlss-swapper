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
    }
}
