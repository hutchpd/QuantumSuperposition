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
        private static readonly AsyncLocal<int> s_depth = new();
        internal static TransactionV2? Current => s_current.Value;
        internal bool IsApplying { get; private set; }

        private static readonly object s_globalFallbackLock = new object();

        private TransactionV2() { ConcurrencyGuard.TransactionStarted(); }

        public static void Run(Action<TransactionV2> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            using var tx = Begin();
            body(tx);
            tx.CommitOnce();
        }

        public static TransactionV2 Begin()
        {
            if (s_current.Value != null)
            {
                // Flat nested semantics: reuse existing ambient
                s_depth.Value = s_depth.Value + 1;
                return s_current.Value;
            }
            var tx = new TransactionV2();
            s_current.Value = tx;
            s_depth.Value = 1;
            return tx;
        }

        public void StageWrite(ITransactionalVariable v, object qb, TxMutationKind kind = TxMutationKind.Append)
        {
            _writeSet[v.TxId] = (v, qb, kind);
        }

        public void RecordRead(ITransactionalVariable v)
        {
            // Record once per variable id to avoid duplicates in validation
            if (_readSet.Any(r => r.var.TxId == v.TxId)) return;
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
#if DEBUG
            // Hooks must operate on immutable data; ensure no direct list refs captured via common patterns is beyond our scope here.
#endif
            _commitHooks.Add(hook);
        }

        // Debug/guard helper: check if a TVar is in the write set of this transaction
        internal bool Contains(ITransactionalVariable v)
        {
            return _writeSet.ContainsKey(v.TxId);
        }

        private void CommitOnce()
        {
            // Optional global fallback under extreme contention
            if (ParanoiaConfig.EnableGlobalFallback && !ParanoiaConfig.FallbackActive)
            {
                long totalPressure = STMTelemetry.TotalRetries + STMTelemetry.TotalAborts;
                if (totalPressure >= ParanoiaConfig.AbortRetryThreshold)
                {
                    ParanoiaConfig.FallbackActive = true;
                }
            }

            if (ParanoiaConfig.FallbackActive)
            {
                lock (s_globalFallbackLock)
                {
                    CommitCore();
                }
                return;
            }

            CommitCore();
        }

        private void CommitCore()
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

                AppendBufferedLedgerEntries();
                RunCommitHooks();
                STMTelemetry.RecordCommit(readOnly: true, writesApplied: 0, lockHoldTicks: 0);
                return;
            }

            var ordered = _writeSet.Values.OrderBy(x => x.var.TxId).ToArray();
            System.Diagnostics.Stopwatch sw = null;

            try
            {
                long lastId = long.MinValue;
                for (int i = 0; i < ordered.Length; i++)
                {
                    var (v, _, _) = ordered[i];
                    // Paranoia: assert strictly increasing TxId order
                    if (ParanoiaConfig.EnableParanoia)
                    {
                        if (v.TxId <= lastId)
                        {
                            throw new InvalidOperationException($"Lock ordering violation: TxId {v.TxId} acquired after {lastId}.");
                        }
                        lastId = v.TxId;
                        ParanoiaConfig.LockStacks[v.TxId] = Environment.StackTrace;
                    }

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
            // Attribute lock hold ticks to each var roughly equally
            long perVarTicks = ordered.Length == 0 ? 0 : ticks / ordered.Length;
            foreach (var (v, _, _) in ordered)
            {
                STMTelemetry.RecordWriteApplied(v.TxId, perVarTicks);
            }

            AppendBufferedLedgerEntries();
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
            int d = s_depth.Value;
            if (d > 1)
            {
                s_depth.Value = d - 1;
                return;
            }
            // root scope end
            s_depth.Value = 0;
            if (ReferenceEquals(s_current.Value, this)) s_current.Value = null;
            ConcurrencyGuard.TransactionEnded();
        }
    }
}
