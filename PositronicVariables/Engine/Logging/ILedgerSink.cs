using System;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// Transactional sink for operation ledger entries with idempotent append semantics.
    /// </summary>
    public interface ILedgerSink
    {
        void Append(IOperation op, Guid commitId);
        IOperation Peek();
        void Pop();
        void ReverseLastOperations();
        void Clear();
    }
}
