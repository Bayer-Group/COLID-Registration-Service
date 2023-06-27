using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class ColidEntryContactInvalidUsersDto
    {
        public string ContactMail { get; set; }
        public IList<ColidEntryInvalidUsersDto> ColidEntries { get; } = new List<ColidEntryInvalidUsersDto>();

        public ColidEntryContactInvalidUsersDto(string contactMail, ColidEntryInvalidUsersDto entry)
        {
            ContactMail = contactMail;
            ColidEntries.Add(entry);
        }
    }
}

