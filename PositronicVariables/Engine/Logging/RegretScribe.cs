using System;
using PositronicVariables.Transactions;
using PositronicVariables.Variables;

namespace PositronicVariables.Engine.Logging
{
    public class RegretScribe<T> : IOperationLogHandler<T> where T : IComparable<T>
    {
        private static ILedgerSink s_sink = new LedgerSink();
        public static ILedgerSink Sink
        {
            get => s_sink;
            set => s_sink = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool SawForwardWrite { get; set; }
        public void Record(IOperation op)
        {
            // During the convergence loop, preserve immediate ledger ordering
            if (PositronicVariable<T>.InConvergenceLoop)
            {
                s_sink.Append(op, Guid.NewGuid());
                return;
            }

            var tx = TransactionV2.Current;
            if (tx != null)
            {
                tx.BufferLedgerEntry(op);
            }
            else
            {
                // outside tx: append immediately with unique id
                s_sink.Append(op, Guid.NewGuid());
            }
        }

        /// <summary>
        /// Undo all operations since the last forward half-cycle marker, a half-cycle being a forward pass followed by a reverse pass, (the 'to' to a Merlin 'fro')
        /// </summary>
        public void UndoLastForwardCycle()
        {
            s_sink.ReverseLastOperations();
        }

        /// <summary>
        /// Sometimes you just need to start fresh and pretend none of that ever happened.
        /// </summary>
        public void Clear()
        {
            s_sink.Clear();
        }
    }
}
