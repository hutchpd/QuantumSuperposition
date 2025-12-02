using System;

namespace PositronicVariables.Engine.Logging
{
    public readonly struct BufferedLedgerEntry
    {
        public readonly IOperation Entry;
        public readonly Guid CommitId;
        public BufferedLedgerEntry(IOperation entry, Guid commitId)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            CommitId = commitId;
        }
    }
}
