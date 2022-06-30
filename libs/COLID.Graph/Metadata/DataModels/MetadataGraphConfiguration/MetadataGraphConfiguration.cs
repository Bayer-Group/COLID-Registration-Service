using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration
{
    [Type(Constants.MetadataGraphConfiguration.Type)]
    public class MetadataGraphConfiguration : Entity
    {
        public IList<string> GetMetadataGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasMetadataGraph);
        }

        public IList<string> GetResourceGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasResourcesGraph);
        }

        public IList<string> GetResourceHistoryGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasResourceHistoryGraph);
        }

        public IList<string> GetConsumerGroupGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasConsumerGroupGraph);
        }

        public IList<string> GetExtendedUriTemplateGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasExtendedUriTemplateGraph);
        }

        public IList<string> GetPidUriTemplateGraphs()
        {
            return GetValue(Constants.MetadataGraphConfiguration.HasPidUriTemplatesGraph);
        }

        private IList<string> GetValue(string key)
        {
            if (Properties.TryGetValue(key, out var graphs))
            {
                return graphs.Cast<string>().ToList();
            }
            return new List<string>();
        }
    }
}
