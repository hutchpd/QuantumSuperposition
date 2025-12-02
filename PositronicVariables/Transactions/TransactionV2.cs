using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Stage 3 transaction: optimistic reads with validation; per-variable locks acquired only at commit.
    /// Includes bounded retries with exponential backoff + jitter.
    /// </summary>
    public sealed class TransactionV2 : IDisposable
    {
        private readonly Dictionary<long, (ITransactionalVariable var, object qb, TxMutationKind kind)> _writeSet = new();
        private readonly List<(ITransactionalVariable var, long version)> _readSet = new();
        private readonly List<BufferedLedgerEntry> _ledgerBuffer = new();
        private readonly List<Action> _commitHooks = new();
        private bool _disposed;

        private static readonly ThreadLocal<Random> s_rng = new(() => new Random(unchecked(Environment.TickCount * 397) ^ Thread.CurrentThread.ManagedThreadId));

        // Ambient transaction reference for guards/diagnostics
        private static readonly AsyncLocal<TransactionV2> s_current = new();
        internal static TransactionV2? Current => s_current.Value;
        internal bool IsApplying { get; private set; }

        private TransactionV2() { ConcurrencyGuard.TransactionStarted(); s_current.Value = this; }

        public static void Run(Action<TransactionV2> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            using var tx = Begin();
            body(tx);
            tx.CommitOnce();
        }

        public static TransactionV2 Begin() => new TransactionV2();

        public void StageWrite(ITransactionalVariable v, object qb, TxMutationKind kind = TxMutationKind.Append)
        {
            _writeSet[v.TxId] = (v, qb, kind);
        }

        public void RecordRead(ITransactionalVariable v)
        {
            _readSet.Add((v, v.TxVersion));
        }

        public void BufferLedgerEntry(IOperation op)
        {
            if (op == null) return;
            _ledgerBuffer.Add(new BufferedLedgerEntry(op, Guid.NewGuid()));
        }

        public void AddCommitHook(Action hook)
        {
            if (hook == null) return;
            _commitHooks.Add(hook);
        }

        // Debug/guard helper: check if a TVar is in the write set of this transaction
        internal bool Contains(ITransactionalVariable v)
        {
            return _writeSet.ContainsKey(v.TxId);
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

                // Append any buffered ledger entries (rare in read-only, but allow)
                AppendBufferedLedgerEntries();

                // Run commit hooks even for read-only (e.g., diagnostics)
                RunCommitHooks();

                STMTelemetry.RecordCommit(readOnly: true, writesApplied: 0, lockHoldTicks: 0);
                return;
            }

            var ordered = _writeSet.Values.OrderBy(x => x.var.TxId).ToArray();
            System.Diagnostics.Stopwatch sw = null;

            try
            {
                for (int i = 0; i < ordered.Length; i++)
                {
                    var (v, _, _) = ordered[i];
                    Monitor.Enter(v.TxLock);
                    if (i == 0)
                    {
                        sw = System.Diagnostics.Stopwatch.StartNew();
                        IsApplying = true; // mark apply phase for guards
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

                foreach (var (v, qb, kind) in ordered)
                {
                    v.TxApply(qb, kind);
                    v.TxBumpVersion();
                }
            }
            finally
            {
                IsApplying = false;
                for (int i = ordered.Length - 1; i >= 0; i--)
                {
                    Monitor.Exit(ordered[i].var.TxLock);
                }
            }

            long ticks = sw?.ElapsedTicks ?? 0;
            STMTelemetry.RecordCommit(readOnly: false, writesApplied: ordered.Length, lockHoldTicks: ticks);

            // After variable locks released, append ledger entries idempotently under ledger lock
            AppendBufferedLedgerEntries();

            // After ledger settled, run commit hooks (e.g., snapshot archival)
            RunCommitHooks();
        }

        private void AppendBufferedLedgerEntries()
        {
            if (_ledgerBuffer.Count == 0) return;
            foreach (var be in _ledgerBuffer)
            {
                Ledger.Sink.Append(be.Entry, be.CommitId);
            }
            _ledgerBuffer.Clear();
        }

        private void RunCommitHooks()
        {
            if (_commitHooks.Count == 0) return;
            foreach (var hook in _commitHooks)
            {
                try { hook(); } catch { }
            }
            _commitHooks.Clear();
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
                    return;
                }
                catch (InvalidOperationException ex) when (ex.Message.StartsWith("STM validation failed", StringComparison.Ordinal))
                {
                    STMTelemetry.RecordRetry();
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (ReferenceEquals(s_current.Value, this)) s_current.Value = null;
            ConcurrencyGuard.TransactionEnded();
        }
    }
}
