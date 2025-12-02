using System;

namespace PositronicVariables.Transactions
{
    public readonly struct HotspotStats
    {
        public long TxId { get; }
        public double AbortRate { get; }
        public double AvgLockHoldTicks { get; }
        public long ValidationFailures { get; }
        public long WritesApplied { get; }

        public HotspotStats(long txId, double abortRate, double avgLockHoldTicks, long validationFailures, long writesApplied)
        {
            TxId = txId;
            AbortRate = abortRate;
            AvgLockHoldTicks = avgLockHoldTicks;
            ValidationFailures = validationFailures;
            WritesApplied = writesApplied;
        }
    }

    public interface IHotspotMitigationStrategy
    {
        bool ShouldMitigate(long tvarId, HotspotStats stats);
        void ApplyMitigation(long tvarId);
    }

    /// <summary>
    /// Simple single-writer promotion strategy: heuristic only, signalling layer decides enforcement.
    /// </summary>
    public sealed class SingleWriterPromotionStrategy : IHotspotMitigationStrategy
    {
        private readonly double _abortThreshold;
        private readonly double _lockTicksThreshold;

        public SingleWriterPromotionStrategy(double abortThreshold = 0.2, double lockTicksThreshold = 10_000)
        {
            _abortThreshold = abortThreshold;
            _lockTicksThreshold = lockTicksThreshold;
        }

        public bool ShouldMitigate(long tvarId, HotspotStats stats)
        {
            return stats.AbortRate > _abortThreshold && stats.AvgLockHoldTicks > _lockTicksThreshold;
        }

        public void ApplyMitigation(long tvarId)
        {
            // No-op placeholder: in a full implementation, coordinator would gate writes to single-writer.
        }
    }
}
