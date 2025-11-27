using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Ambient, async-friendly transactional scope with flat nesting and buffered commit hooks.
    /// Uses optimistic validation and per-variable deterministic locking at commit.
    /// </summary>
    public sealed class TransactionScope : IDisposable
    {
        private sealed class TxData
        {
            public readonly Dictionary<long, (ITransactionalVariable var, object qb)> WriteSet = new();
            public readonly List<(ITransactionalVariable var, long version)> ReadSet = new();
            public readonly List<Action> Hooks = new();
        }

        private static readonly AsyncLocal<TxData?> s_current = new();
        private static readonly AsyncLocal<int> s_depth = new();
        private static readonly ThreadLocal<Random> s_rng = new(() => new Random(unchecked(Environment.TickCount * 397) ^ Thread.CurrentThread.ManagedThreadId));

        private readonly bool _isRoot;
        private readonly int _maxAttempts;
        private readonly int _baseDelayMs;
        private readonly int _maxDelayMs;
        private bool _disposed;

        private TransactionScope(bool isRoot, int maxAttempts, int baseDelayMs, int maxDelayMs)
        {
            _isRoot = isRoot;
            _maxAttempts = maxAttempts;
            _baseDelayMs = baseDelayMs;
            _maxDelayMs = maxDelayMs;
        }

        public static TransactionScope Begin(int maxAttempts = 8, int baseDelayMs = 1, int maxDelayMs = 50)
        {
            if (s_current.Value == null)
            {
                s_current.Value = new TxData();
                s_depth.Value = 1;
                return new TransactionScope(isRoot: true, maxAttempts, baseDelayMs, maxDelayMs);
            }
            else
            {
                s_depth.Value = s_depth.Value + 1;
                return new TransactionScope(isRoot: false, maxAttempts, baseDelayMs, maxDelayMs);
            }
        }

        public static void RecordRead(ITransactionalVariable v)
        {
            var data = s_current.Value ?? throw new InvalidOperationException("No ambient transaction. Use TransactionScope.Begin().");
            data.ReadSet.Add((v, v.TxVersion));
        }

        public static void StageWrite(ITransactionalVariable v, object qb)
        {
            var data = s_current.Value ?? throw new InvalidOperationException("No ambient transaction. Use TransactionScope.Begin().");
            data.WriteSet[v.TxId] = (v, qb);
        }

        public static void AddCommitHook(Action hook)
        {
            if (hook == null) throw new ArgumentNullException(nameof(hook));
            var data = s_current.Value ?? throw new InvalidOperationException("No ambient transaction. Use TransactionScope.Begin().");
            data.Hooks.Add(hook);
        }

        public static void Run(Action body, int maxAttempts = 8, int baseDelayMs = 1, int maxDelayMs = 50)
        {
            using var tx = Begin(maxAttempts, baseDelayMs, maxDelayMs);
            body();
        }

        public static async Task RunAsync(Func<Task> body, int maxAttempts = 8, int baseDelayMs = 1, int maxDelayMs = 50)
        {
            using var tx = Begin(maxAttempts, baseDelayMs, maxDelayMs);
            await body().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (s_depth.Value <= 0)
            {
                return;
            }

            if (_isRoot)
            {
                try
                {
                    CommitWithRetry();
                }
                finally
                {
                    s_current.Value = null;
                    s_depth.Value = 0;
                }
            }
            else
            {
                s_depth.Value = s_depth.Value - 1;
            }
        }

        private void CommitWithRetry()
        {
            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    CommitOnce();
                    return;
                }
                catch (InvalidOperationException ex) when (ex.Message.StartsWith("STM validation failed", StringComparison.Ordinal))
                {
                    if (attempt == _maxAttempts)
                        throw new TransactionAbortedException("Transaction aborted after maximum retries due to contention.", attempt);

                    int delay = Math.Min(_maxDelayMs, (int)(Math.Pow(2, attempt - 1) * _baseDelayMs));
                    int jitter = s_rng.Value!.Next(0, 3);
                    Thread.Sleep(delay + jitter);
                }
            }
        }

        private void CommitOnce()
        {
            var data = s_current.Value ?? throw new InvalidOperationException("No ambient transaction.");
            var ordered = data.WriteSet.Values.OrderBy(x => x.var.TxId).ToArray();

            try
            {
                foreach (var (v, _) in ordered)
                {
                    Monitor.Enter(v.TxLock);
                }

                var writeIds = data.WriteSet.Keys.ToHashSet();
                foreach (var (v, ver) in data.ReadSet)
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

            foreach (var hook in data.Hooks)
            {
                try { hook(); } catch { }
            }
        }
    }
}
