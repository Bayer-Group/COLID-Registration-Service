using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.TransferObjects
{
    public class ColidUserDto : DtoBase
    {
       
        #region user related

        public Guid id { get; set; }

        public string EmailAddress { get; set; }

        #endregion user related
    }
}
