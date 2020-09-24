using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration
{
    public class MetadataGraphConfigurationOverviewDTO
    {
        public string Id { get; set; }
        public string StartDateTime { get; set; }
        public string EditorialNote { get; set; }
        public IEnumerable<string> Graphs { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
