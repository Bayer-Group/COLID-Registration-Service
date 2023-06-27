using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class InvalidDistributionEndpointContactResourceInformation : InvalidContactResourceInformation
    {
        public string DistributionId { get; set; }
        public string DistributionLabel { get; set; }
        public string InvalidDistributionEndpointContact { get; set; }
    }
}

