using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IEndpointTestService
    {
        void PushEndpointsInQueue();
        void PushSingleEndpointInQueue(Uri distributionPidUri);

        void TestEndpoints(string mqValue);

        IList<DistributionEndpointsTest> GetBrokenEndpoints();
    }
}
