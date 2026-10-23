using System;

namespace PositronicVariables.Transactions
{
    public sealed class TransactionAbortedException : Exception
    {
        public int Attempts { get; }
        public TransactionAbortedException(string message, int attempts) : base(message)
        {
            Attempts = attempts;
        }
    }

    internal sealed class StmValidationFailedException : InvalidOperationException
    {
        public StmValidationFailedException(string message) : base(message) { }
    }
}
