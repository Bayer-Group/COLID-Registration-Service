using System.Net;
using COLID.Exception.Attributes;
using COLID.Exception.Models;

namespace COLID.Identity.Exception
{
    [StatusCode(HttpStatusCode.Forbidden)]
    public class AuthorizationException : BusinessException
    {
        public AuthorizationException(string message) : base(message)
        {
        }

        public AuthorizationException(string message, System.Exception inner) : base(message, inner)
        {
        }
    }
}
