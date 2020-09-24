using System;

namespace COLID.Exception.Models
{
    public class TechnicalException : GeneralException
    {
        public TechnicalException()
        {
        }

        public TechnicalException(string message)
            : base(message)
        {
        }

        public TechnicalException(string message, System.Exception inner)
            : base(message, inner)
        {
        }
    }
}
