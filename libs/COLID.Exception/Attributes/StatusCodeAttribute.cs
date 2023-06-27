using System;
using System.Net;

namespace COLID.Exception.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public sealed class StatusCodeAttribute : Attribute
    {
        private HttpStatusCode _httpStatusCode;

#pragma warning disable CA1019 // Define accessors for attribute arguments
        public StatusCodeAttribute(HttpStatusCode httpStatusCode)
#pragma warning restore CA1019 // Define accessors for attribute arguments
        {
            _httpStatusCode = httpStatusCode;
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public int GetCode()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            return (int)_httpStatusCode;
        }
    }
}
