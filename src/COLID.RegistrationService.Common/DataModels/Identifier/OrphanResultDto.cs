using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModel.Identifier
{
    public class OrphanResultDto
    {
        public string PidUri { get; private set; }

        public string Message { get; private set; }

        public bool Success { get; private set; }

        public OrphanResultDto(string pidUri, string message, bool success)
        {
            PidUri = pidUri;
            Message = message;
            Success = success;
        }
    }
}
