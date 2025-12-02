using System;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// Default ledger sink delegating to QuantumLedgerOfRegret with idempotent append.
    /// Think of it as a responsible adult supervising your regrets.
    /// </summary>
    public sealed class LedgerSink : ILedgerSink
    {
        public void Append(IOperation op, Guid commitId) => QuantumLedgerOfRegret.Append(op, commitId);
        public IOperation Peek() => QuantumLedgerOfRegret.Peek();
        public void Pop() => QuantumLedgerOfRegret.Pop();
        public void ReverseLastOperations() => QuantumLedgerOfRegret.ReverseLastOperations();
        public void Clear() => QuantumLedgerOfRegret.Clear();
    }
}
