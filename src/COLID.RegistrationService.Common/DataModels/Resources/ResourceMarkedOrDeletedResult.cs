using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class ResourceMarkedOrDeletedResult
    {
        public Uri PidUri { get; private set; }

        public string Message { get; private set; }

        public bool Success { get; private set; }

        public ResourceMarkedOrDeletedResult(Uri pidUri, string message, bool success)
        {
            PidUri = pidUri;
            Message = message;
            Success = success;
        }
    }
}
