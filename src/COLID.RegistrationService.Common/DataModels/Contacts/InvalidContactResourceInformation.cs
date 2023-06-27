using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class InvalidContactResourceInformation
    {
        public Uri PidUri { get; set; }
        public string Label { get; set; }
        public string LastReviewer { get; set; }
        public string LastChangeUser { get; set; }
        public string Author { get; set; }
        public string ConsumerGroupContactPerson { get; set; }
    }
}

