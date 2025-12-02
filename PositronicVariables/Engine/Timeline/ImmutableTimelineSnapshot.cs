using QuantumSuperposition.QuantumSoup;
using System;

namespace PositronicVariables.Engine.Timeline
{
    public sealed class ImmutableTimelineSnapshot<T>
        where T : IComparable<T>
    {
        public long PositronicVariableId { get; }
        public long Version { get; }
        public QuBit<T>[] Slices { get; }
        public DateTimeOffset Timestamp { get; }

        public ImmutableTimelineSnapshot(long variableId, long version, QuBit<T>[] slices)
        {
            PositronicVariableId = variableId;
            Version = version;
            // Store clones to preserve immutability
            Slices = slices ?? Array.Empty<QuBit<T>>();
            Timestamp = DateTimeOffset.UtcNow;
        }
    }
}
