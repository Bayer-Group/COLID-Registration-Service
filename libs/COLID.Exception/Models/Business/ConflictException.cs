using System;
using System.Net;
using COLID.Exception.Attributes;
using Newtonsoft.Json;

namespace COLID.Exception.Models.Business
{
    [StatusCode(HttpStatusCode.Conflict)]
    public class ConflictException : BusinessException
    {
        public ConflictException(string message) : base(message) { }

        public ConflictException(string message, System.Exception innerException) : base(message, innerException) { }

        public ConflictException() { }
    }
}
