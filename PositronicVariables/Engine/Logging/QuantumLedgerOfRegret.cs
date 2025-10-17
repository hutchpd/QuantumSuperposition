using System.Collections.Generic;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// The Quantum Ledger of Regret™ — remembers every dumb thing you've done so you can go back and pretend you didn't.
    /// </summary>
    public static class QuantumLedgerOfRegret
    {
        private static readonly Stack<IOperation> _log = new();

        public static void Record(IOperation op)
        {
            _log.Push(op);
        }

        public static IOperation Peek()
        {
            return _log.Count > 0 ? _log.Peek() : null;
        }

        public static void ReverseLastOperations()
        {
            while (_log.Count > 0)
            {
                IOperation op = _log.Pop();
                op.Undo();
            }
        }

        // violently yeet the last recorded mistake into the entropy void
        public static void Pop()
        {
            if (_log.Count > 0)
            {
                _ = _log.Pop();
            }
        }

        public static void Clear()
        {
            _log.Clear();
        }
    }
}
