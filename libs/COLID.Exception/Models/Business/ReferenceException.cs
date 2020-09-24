using System;
using System.Net;
using COLID.Exception.Attributes;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    [StatusCode(HttpStatusCode.Conflict)]
    public class ReferenceException : BusinessException
    {
        [JsonProperty]
        public string ReferenceId { get; set; }

        public ReferenceException(string message, string referenceId) : base(message)
        {
            ReferenceId = referenceId;
        }

        public ReferenceException(string message, string referenceId, System.Exception innerException) : base(message, innerException)
        {
            ReferenceId = referenceId;
        }

        public ReferenceException() { }
    }
}
