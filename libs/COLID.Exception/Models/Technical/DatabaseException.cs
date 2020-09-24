using System;

namespace COLID.Exception.Models.Technical
{
    public class DatabaseException : TechnicalException
    {
        public DatabaseException(string message) : base(message)
        {
        }

        public DatabaseException(string message, System.Exception inner) : base(message, inner)
        {
        }
    }
}
