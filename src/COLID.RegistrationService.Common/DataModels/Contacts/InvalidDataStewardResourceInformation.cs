using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class InvalidDataStewardResourceInformation : InvalidContactResourceInformation
    {
        public ISet<string> InvalidDataStewards { get; } = new HashSet<string>();
        public ISet<string> ValidDataStewards { get; } = new HashSet<string>();
    }
}

