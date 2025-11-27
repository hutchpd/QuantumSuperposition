using System;
using System.Collections.Generic;
using PositronicVariables.Transactions;

namespace PositronicVariables.Engine.Coordinator
{
    public interface IConvergenceWorkItem
    {
        void BuildWrites(TransactionV2 tx);
        IEnumerable<Action> BuildCommitHooks();
        object? GetResultAfterCommit();
    }
}
