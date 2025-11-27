using System;
using System.Threading;

namespace PositronicVariables.Transactions
{
    internal static class ConcurrencyGuard
    {
        private static int _activeTransactions;
        private static int _coordinatorThreadId = -1;
        private static int _engineThreadId = -1;
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
        public static void RegisterEngineThread()
        {
            _engineThreadId = Environment.CurrentManagedThreadId;
        }
        public static bool IsCoordinatorThread => Environment.CurrentManagedThreadId == _coordinatorThreadId;
        public static bool IsEngineThread => Environment.CurrentManagedThreadId == _engineThreadId;

#if DEBUG
        public static void AssertConvergenceEntrySafe()
        {
            if (ActiveTransactions > 0 && !IsCoordinatorThread)
            {
                throw new InvalidOperationException("Convergence loop entered while transactions are active and caller is not coordinator thread.");
            }
        }
#endif
    }
}
