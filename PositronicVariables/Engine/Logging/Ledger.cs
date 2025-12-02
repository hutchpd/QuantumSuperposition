using System;

namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// Global access point for the process-wide ledger sink.
    /// </summary>
    public static class Ledger
    {
        private static ILedgerSink _sink = new LedgerSink();
        public static ILedgerSink Sink
        {
            get => _sink;
            set => _sink = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
