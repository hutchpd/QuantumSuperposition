using System;
using System.Threading;
using PositronicVariables.Variables;

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

        public static void RegisterCoordinatorThread() => _coordinatorThreadId = Environment.CurrentManagedThreadId;
        public static void RegisterEngineThread() => _engineThreadId = Environment.CurrentManagedThreadId;
        public static void UnregisterCoordinatorThread() => _coordinatorThreadId = -1;
        public static void UnregisterEngineThread() => _engineThreadId = -1;
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

        public static void AssertTimelineMutationContext<T>(PositronicVariable<T> variable) where T : IComparable<T>
        {
            var currentTx = TransactionV2.Current;
            bool inApply = currentTx != null && currentTx.IsApplying;
            bool hasLock = Monitor.IsEntered(((ITransactionalVariable)variable).TxLock);

            if (inApply)
            {
                if (currentTx == null || !currentTx.Contains((ITransactionalVariable)variable))
                {
                    throw new InvalidOperationException("Unsafe timeline mutation: TVar not in current transaction write set during apply.");
                }
                return;
            }

            if (hasLock)
            {
                return;
            }

            if (IsEngineThread || IsCoordinatorThread)
            {
                return;
            }

            if (ActiveTransactions == 0 || AllowNonTransactionalWrites)
            {
                return;
            }

            throw new InvalidOperationException(
                "Unsafe timeline mutation: must occur during TransactionV2 commit, under TVar lock, or on the engine/coordinator thread.");
        }
#endif
    }
}
