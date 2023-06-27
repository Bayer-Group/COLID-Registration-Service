using System;
using System.Net;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Common.DataModel.DistributionEndpoints
{
    public class DistributionEndpointsTest
    {
        public string Author { get; set; }
        public string PidUri { get; set; }
        public string NetworkAddress { get; set; }
        public string ResourceLabel { get; set; }
        public string DistributionEndpointSubject { get; set; } //subject
        public string ConsumerGroupContactPerson { get; set; } 

        public string LastChangeUser { get; set; }

        public bool CheckedFlag { get; set; } = false; //false

#pragma warning disable CA2225 // Operator overloads have named alternates
        public static explicit operator EndPoint(DistributionEndpointsTest v)
#pragma warning restore CA2225 // Operator overloads have named alternates
        {
            throw new NotImplementedException();
        }
    }
}
