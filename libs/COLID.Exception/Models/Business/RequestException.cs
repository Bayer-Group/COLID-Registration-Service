using System;

namespace COLID.Exception.Models.Business
{
    public class RequestException : BusinessException
    {
        public RequestException() : base()
        {
        }
        public RequestException(string message) : base(message)
        {
        }

        public RequestException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
