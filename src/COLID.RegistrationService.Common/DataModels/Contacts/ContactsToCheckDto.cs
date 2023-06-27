using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class ContactsToCheckDto
    {
        public ISet<string> DataStewards { get; } = new HashSet<string>();
        public ISet<string> DistributionEndpointContactPersons { get; } = new HashSet<string>();
    }
}

