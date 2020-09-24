using System.Net;
using COLID.Exception.Attributes;

namespace COLID.Exception.Models
{
    [StatusCode(HttpStatusCode.BadRequest)]
    public class BusinessException : GeneralException
    {
        public BusinessException()
        {
        }

        public BusinessException(string message)
            : base(message)
        {
        }

        public BusinessException(string message, System.Exception inner)
            : base(message, inner)
        {
        }
    }
}
