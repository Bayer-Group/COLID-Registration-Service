using System;

namespace COLID.Exception.Models.Technical
{
    public class TransactionNotCommittedException : TechnicalException
    {
        public TransactionNotCommittedException(string message)
            : base(message)
        {
        }

        public TransactionNotCommittedException(string message, System.Exception inner)
            : base(message, inner)
        {
        }
    }
}
