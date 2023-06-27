using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class ColidEntryInvalidUsersDto
    {
        public Uri PidUri { get; set; }
        public string Label { get; set; }
        public IEnumerable<string> InvalidUsers { get; set; }

        public ColidEntryInvalidUsersDto(Uri pidUri, string label, IEnumerable<string> invalidUsers)
        {
            PidUri = pidUri;
            Label = label;
            InvalidUsers = invalidUsers;
        }
    }
}

