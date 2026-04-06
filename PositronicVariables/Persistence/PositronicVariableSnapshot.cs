using System;
using System.Collections.Generic;

namespace PositronicVariables.Persistence
{
    public sealed class PositronicVariableSnapshot<T>
        where T : IComparable<T>
    {
        public long SourceVariableId { get; set; }
        public long Version { get; set; }
        public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
        public List<T[]> TimelineSlices { get; set; } = [];
    }
}