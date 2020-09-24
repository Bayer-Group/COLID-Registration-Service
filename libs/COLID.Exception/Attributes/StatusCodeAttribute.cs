using System;
using System.Net;

namespace COLID.Exception.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public class StatusCodeAttribute : Attribute
    {
        private HttpStatusCode _httpStatusCode;

        public StatusCodeAttribute(HttpStatusCode httpStatusCode)
        {
            _httpStatusCode = httpStatusCode;
        }

        public int GetCode()
        {
            return (int)_httpStatusCode;
        }
    }
}
