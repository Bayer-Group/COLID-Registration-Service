using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModel.DistributionEndpoints
{
    public class InvalidDistributionEndpointMessage : DistributionEndpoint
    {
        public string UserEmail { get; set; }
        public Uri DistributionEndpoint { get; set; }
        public string ResourceLabel { get; set; }
        public string DistributionEndpointPidUri { get; set; }
    }
}
