using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Engine.Timeline
{
    /// <summary>
    /// Operation that reverts the timeline to a previous snapshot after a Replace.
    /// </summary>
    public class TimelineReplaceOperation<T> : IOperation
        where T : IComparable<T>
    {
        private readonly List<QuBit<T>> _backupTimeline;

        public PositronicVariable<T> Variable { get; }
        internal QuBit<T> ReplacedSlice => _backupTimeline[^1];

        public string OperationName => "TimelineReplace";

        public TimelineReplaceOperation(PositronicVariable<T> variable, List<QuBit<T>> backupTimeline)
        {
            Variable = variable;
            _backupTimeline = backupTimeline
                .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                .ToList();
        }

        public void Undo()
        {
            Variable.timeline.Clear();
            Variable.timeline.AddRange(_backupTimeline);
        }
    }
}
