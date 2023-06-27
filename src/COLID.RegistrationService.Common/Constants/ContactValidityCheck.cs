using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.Constants
{
    public static class ContactValidityCheck
    {
        public static readonly string ServiceUrl = Settings.GetServiceUrl();
        public static readonly string BrokenDataStewards = ServiceUrl + "kos/19050/hasBrokenDataSteward";
        public static readonly string BrokenEndpointContacts = ServiceUrl + "kos/19050/hasBrokenEndpointContact";
    }
}

