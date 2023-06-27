using System.Linq;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.TripleStore.DataModels.ConsumerGroups {
    public class ConsumerGroupResultDTO : BaseEntityResultDTO
    {
        public string LifecycleStatus => GetLifecycleStatus();

        private string GetLifecycleStatus()
        {
            return Properties.TryGetValue(COLID.Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus, out var lifecycleStatusList) ? lifecycleStatusList.FirstOrDefault() : null;
        }
    }
}
