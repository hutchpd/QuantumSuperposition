using System;
using System.Threading;

namespace PositronicVariables.Transactions
{
    /// <summary>
    /// Coarse-grained transactional guard. Stage 1: serialize transactional bodies using a global lock.
    /// </summary>
    public sealed class Transaction : IDisposable
    {
        private static readonly object GlobalTxLock = new();
        private bool _owns;
        private bool _completed;

        private Transaction(bool owns)
        {
            _owns = owns;
        }

        /// <summary>
        /// Begin a transaction by acquiring the global lock.
        /// </summary>
        public static Transaction Begin()
        {
            Monitor.Enter(GlobalTxLock);
            return new Transaction(owns: true);
        }

        /// <summary>
        /// Runs the body while holding the global transaction lock.
        /// </summary>
        public static void Run(Action<Transaction> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            using Transaction tx = Begin();
            body(tx);
            tx.Commit();
        }

        /// <summary>
        /// Commit the transaction and release the lock.
        /// </summary>
        public void Commit()
        {
            if (_completed) return;
            _completed = true;
            Release();
        }

        private void Release()
        {
            if (_owns)
            {
                _owns = false;
                Monitor.Exit(GlobalTxLock);
            }
        }

        public void Dispose()
        {
            // If user forgot to Commit, treat Dispose as commit boundary and release the lock.
            Release();
        }
    }
}
