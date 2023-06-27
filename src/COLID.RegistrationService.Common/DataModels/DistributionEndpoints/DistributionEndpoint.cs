using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Common.DataModel.DistributionEndpoints
{
    public class DistributionEndpoint : Entity
    {
        /// <summary>
        /// The PID URI of the corresponding COLID entry.
        /// </summary>
        public string ColidEntryPidUri { get; set; }
    }
}
