namespace PositronicVariables.Engine.Logging
{
    public class RegretScribe<T> : IOperationLogHandler<T>
    {
        public bool SawForwardWrite { get; set; }
        public void Record(IOperation op)
        {
            QuantumLedgerOfRegret.Record(op);
        }

        /// <summary>
        /// Undo all operations since the last forward half-cycle marker, a half-cycle being a forward pass followed by a reverse pass, (the 'to' to a Merlin 'fro')
        /// </summary>
        public void UndoLastForwardCycle()
        {
            QuantumLedgerOfRegret.ReverseLastOperations();
        }

        /// <summary>
        /// Sometimes you just need to start fresh and pretend none of that ever happened.
        /// </summary>
        public void Clear()
        {
            QuantumLedgerOfRegret.Clear();
        }
    }
}
