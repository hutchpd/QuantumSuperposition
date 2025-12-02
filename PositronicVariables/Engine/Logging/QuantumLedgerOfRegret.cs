using System;
using System.Collections.Generic;
using System.Threading;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// The Quantum Ledger of Regret™ — remembers every dumb thing you've done so you can go back and pretend you didn't.
    /// Now with idempotent, commit-identified append semantics so your mistakes aren’t recorded twice by an overenthusiastic historian.
    /// </summary>
    public static class QuantumLedgerOfRegret
    {
        private static readonly Stack<IOperation> _log = new();
        private static readonly object _lock = new();
        private static readonly HashSet<Guid> _seenCommitIds = new();

        /// <summary>
        /// Legacy direct record. Prefer buffering via TransactionV2 and appending via Append(op, commitId).
        /// Useful in emergencies and overly dramatic unit tests.
        /// </summary>
        public static void Record(IOperation op)
        {
            if (op == null) return;
            Append(op, Guid.NewGuid());
        }

        /// <summary>
        /// Idempotent append guarded by commit id. If the commit id was already seen, the entry is ignored.
        /// This prevents déjà vu from becoming data duplication.
        /// </summary>
        public static void Append(IOperation op, Guid commitId)
        {
            if (op == null) return;
            lock (_lock)
            {
                if (_seenCommitIds.Contains(commitId)) return;
                _seenCommitIds.Add(commitId);
                _log.Push(op);
            }
        }

        public static IOperation Peek()
        {
            lock (_lock)
            {
                return _log.Count > 0 ? _log.Peek() : null;
            }
        }

        public static void ReverseLastOperations()
        {
            lock (_lock)
            {
                while (_log.Count > 0)
                {
                    IOperation op = _log.Pop();
                    op.Undo();
                }
            }
        }

        // Violently yeet the last recorded mistake into the entropy void.
        public static void Pop()
        {
            lock (_lock)
            {
                if (_log.Count > 0)
                {
                    _ = _log.Pop();
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _log.Clear();
                _seenCommitIds.Clear();
            }
        }
    }
}
