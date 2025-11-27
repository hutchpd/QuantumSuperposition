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
    public sealed class TransactionV2 : IDisposable
    {
        private readonly Dictionary<long, (ITransactionalVariable var, object qb)> _writeSet = new();
        private readonly List<(ITransactionalVariable var, long version)> _readSet = new();
        private bool _disposed;

        private static readonly ThreadLocal<Random> s_rng = new(() => new Random(unchecked(Environment.TickCount * 397) ^ Thread.CurrentThread.ManagedThreadId));

        private TransactionV2() { ConcurrencyGuard.TransactionStarted(); }

        public static void Run(Action<TransactionV2> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            using var tx = Begin();
            body(tx);
            tx.CommitOnce();
        }

        public static void RunWithRetry(Action<TransactionV2> body, int maxAttempts = 8, int baseDelayMs = 1, int maxDelayMs = 50)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var tx = Begin();
                try
                {
                    body(tx);
                    tx.CommitOnce();
                    return; // success
                }
                catch (InvalidOperationException ex) when (ex.Message.StartsWith("STM validation failed", StringComparison.Ordinal))
                {
                    STMTelemetry.RecordRetry();
                    // backoff
                    if (attempt == maxAttempts)
                    {
                        STMTelemetry.RecordAbort();
                        throw new TransactionAbortedException("Transaction aborted after maximum retries due to contention.", attempt);
                    }

                    int delay = Math.Min(maxDelayMs, (int)(Math.Pow(2, attempt - 1) * baseDelayMs));
                    int jitter = s_rng.Value!.Next(0, 3);
                    Thread.Sleep(delay + jitter);
                }
            }
        }

        public static TransactionV2 Begin() => new TransactionV2();

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
            // Read-only fast path: validate read versions, no locks
            if (_writeSet.Count == 0)
            {
                foreach (var (v, ver) in _readSet)
                {
                    if (v.TxVersion != ver)
                    {
                        STMTelemetry.RecordValidationFailure(v.TxId);
                        throw new InvalidOperationException("STM validation failed: read version changed.");
                    }
                }

                STMTelemetry.RecordCommit(readOnly: true, writesApplied: 0, lockHoldTicks: 0);
                return;
            }

            var ordered = _writeSet.Values.OrderBy(x => x.var.TxId).ToArray();
            System.Diagnostics.Stopwatch sw = null;

            try
            {
                for (int i = 0; i < ordered.Length; i++)
                {
                    var (v, _) = ordered[i];
                    Monitor.Enter(v.TxLock);
                    if (i == 0)
                    {
                        sw = System.Diagnostics.Stopwatch.StartNew();
                    }
                }

                var writeIds = _writeSet.Keys.ToHashSet();
                foreach (var (v, ver) in _readSet)
                {
                    if (writeIds.Contains(v.TxId)) continue;
                    if (v.TxVersion != ver)
                    {
                        STMTelemetry.RecordValidationFailure(v.TxId);
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

            long ticks = sw?.ElapsedTicks ?? 0;
            STMTelemetry.RecordCommit(readOnly: false, writesApplied: ordered.Length, lockHoldTicks: ticks);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ConcurrencyGuard.TransactionEnded();
        }
    }
}
