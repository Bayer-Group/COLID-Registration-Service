using System;
using COLID.Exception.Models;

namespace COLID.Exception.Models.Business
{
    public class DeprecatedVersionException : BusinessException
    {
        public DeprecatedVersionException()
        { 
        }

        public DeprecatedVersionException(string message) : base(message)
        {
        }

        public DeprecatedVersionException(string message, System.Exception inner) : base(message, inner)
        {
        }
    }
}
