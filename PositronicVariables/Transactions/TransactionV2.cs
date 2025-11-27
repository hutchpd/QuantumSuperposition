using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Stage 2 transaction: per-variable locks (TVar) with deterministic lock ordering.
    /// Optimistic reads + validate + apply.
    /// </summary>
    public sealed class TransactionV2
    {
        private readonly Dictionary<ITransactionalVariable, object> _writeSet = new();
        private readonly List<(ITransactionalVariable var, long version)> _readSet = new();

        public static void Run(Action<TransactionV2> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            var tx = new TransactionV2();
            body(tx);
            tx.Commit();
        }

        public void StageWrite(ITransactionalVariable v, object qb)
        {
            _writeSet[v] = qb;
        }

        public void RecordRead(ITransactionalVariable v)
        {
            _readSet.Add((v, v.TxVersion));
        }

        public void Commit()
        {
            // Order locks by ascending id
            var writeVars = _writeSet.Keys.OrderBy(v => v.TxId).ToArray();
            try
            {
                foreach (var v in writeVars)
                {
                    Monitor.Enter(v.TxLock);
                }

                // Validate read set (ignore any that are also in write set)
                foreach (var (v, ver) in _readSet)
                {
                    if (_writeSet.ContainsKey(v)) continue;
                    if (v.TxVersion != ver)
                    {
                        throw new InvalidOperationException("STM validation failed: read version changed.");
                    }
                }

                // Apply writes
                foreach (var (v, qb) in _writeSet)
                {
                    v.TxApplyRequired(qb);
                    v.TxBumpVersion();
                }
            }
            finally
            {
                for (int i = writeVars.Length - 1; i >= 0; i--)
                {
                    Monitor.Exit(writeVars[i].TxLock);
                }
            }
        }
    }
}
