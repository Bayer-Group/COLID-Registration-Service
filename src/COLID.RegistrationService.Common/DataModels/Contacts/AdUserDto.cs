using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Contacts
{
    public class AdUserDto
    {
        public string Id { get; set; }
        public string Mail { get; set; }
        public bool AccountEnabled { get; set; }

        public AdUserDto(string id, string mail, bool accountEnabled)
        {
            Id = id;
            Mail = mail;
            AccountEnabled = accountEnabled;
        }
    }
}

