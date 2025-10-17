using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Engine.Timeline
{
    /// <summary>
    /// Yanks the timeline back to a simpler time, before it made any questionable life choices.
    /// </summary>
    public class TimelineAppendOperation<T> : IOperation
        where T : IComparable<T>
    {
        private readonly List<QuBit<T>> _backupTimeline;
        public PositronicVariable<T> Variable { get; }
        public QuBit<T> AddedSlice { get; }

        public string OperationName => "TimelineAppend";

        public TimelineAppendOperation(PositronicVariable<T> variable, List<QuBit<T>> backupTimeline, QuBit<T> added)
        {
            Variable = variable;
            // Make a safe copy so we can fully restore:
            _backupTimeline = backupTimeline
                .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                .ToList();
            AddedSlice = added;

        }

        public void Undo()
        {
            Variable.timeline.Clear();
            Variable.timeline.AddRange(_backupTimeline);
        }
    }
}
