using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IResourceDataValidityCheckService
    {
        void PushContactsToCheckInQueue();
        void PushEndpointsInQueue();
        void PushSingleEndpointInQueue(Uri distributionPidUri);
        void PushDataFlaggingInQueue();

        IList<DistributionEndpointsTest> GetBrokenEndpoints();
        IList<Uri> GetPidUrisForInvalidDataResources();
    }
}
