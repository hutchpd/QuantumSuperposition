using System;
using System.Threading;

namespace PositronicVariables.Transactions
{
    internal static class ConcurrencyGuard
    {
        private static int _activeTransactions;
        private static int _coordinatorThreadId = -1;
        private static bool _allowNonTransactionalWrites;

        public static int ActiveTransactions => Volatile.Read(ref _activeTransactions);
        public static bool AllowNonTransactionalWrites
        {
            get => _allowNonTransactionalWrites;
            set => _allowNonTransactionalWrites = value;
        }
        public static void TransactionStarted() => Interlocked.Increment(ref _activeTransactions);
        public static void TransactionEnded() => Interlocked.Decrement(ref _activeTransactions);

        public static void RegisterCoordinatorThread()
        {
            _coordinatorThreadId = Environment.CurrentManagedThreadId;
        }
        public static bool IsCoordinatorThread => Environment.CurrentManagedThreadId == _coordinatorThreadId;

#if DEBUG
        public static void AssertConvergenceEntrySafe()
        {
            if (ActiveTransactions > 0)
            {
                throw new InvalidOperationException("Convergence loop entered while transactions are active. This is disallowed in Stage A.");
            }
        }
#endif
    }
}
