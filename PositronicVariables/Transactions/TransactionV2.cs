using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Stage 3 transaction: optimistic reads with validation; per-variable locks acquired only at commit.
    /// Includes bounded retries with exponential backoff + jitter.
    /// </summary>
    public sealed class TransactionV2
    {
        private readonly Dictionary<long, (ITransactionalVariable var, object qb)> _writeSet = new();
        private readonly List<(ITransactionalVariable var, long version)> _readSet = new();

        private static readonly ThreadLocal<Random> s_rng = new(() => new Random(unchecked(Environment.TickCount * 397) ^ Thread.CurrentThread.ManagedThreadId));

        public static void Run(Action<TransactionV2> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            var tx = new TransactionV2();
            body(tx);
            tx.CommitOnce();
        }

        public static void RunWithRetry(Action<TransactionV2> body, int maxAttempts = 8, int baseDelayMs = 1, int maxDelayMs = 50)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var tx = new TransactionV2();
                try
                {
                    body(tx);
                    tx.CommitOnce();
                    return; // success
                }
                catch (InvalidOperationException ex) when (ex.Message.StartsWith("STM validation failed", StringComparison.Ordinal))
                {
                    // backoff
                    if (attempt == maxAttempts)
                    {
                        throw new TransactionAbortedException("Transaction aborted after maximum retries due to contention.", attempt);
                    }

                    int delay = Math.Min(maxDelayMs, (int)(Math.Pow(2, attempt - 1) * baseDelayMs));
                    int jitter = s_rng.Value!.Next(0, 3);
                    Thread.Sleep(delay + jitter);
                }
            }
        }

        public void StageWrite(ITransactionalVariable v, object qb)
        {
            _writeSet[v.TxId] = (v, qb);
        }

        public void RecordRead(ITransactionalVariable v)
        {
            _readSet.Add((v, v.TxVersion));
        }

        private void CommitOnce()
        {
            var ordered = _writeSet.Values.OrderBy(x => x.var.TxId).ToArray();
            try
            {
                foreach (var (v, _) in ordered)
                {
                    Monitor.Enter(v.TxLock);
                }

                var writeIds = _writeSet.Keys.ToHashSet();
                foreach (var (v, ver) in _readSet)
                {
                    if (writeIds.Contains(v.TxId)) continue;
                    if (v.TxVersion != ver)
                    {
                        throw new InvalidOperationException("STM validation failed: read version changed.");
                    }
                }

                foreach (var (v, qb) in ordered)
                {
                    v.TxApplyRequired(qb);
                    v.TxBumpVersion();
                }
            }
            finally
            {
                for (int i = ordered.Length - 1; i >= 0; i--)
                {
                    Monitor.Exit(ordered[i].var.TxLock);
                }
            }
        }
    }
}
