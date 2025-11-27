using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Engine.Timeline
{
    public class BureauOfTemporalRecords<T> : ITimelineArchivist<T>
        where T : IComparable<T>
    {
        private readonly Stack<(PositronicVariable<T> Variable, List<QuBit<T>> Timeline)> _snapshots = new();
        private readonly object _syncRoot = new();
        private Action _onAppend;

        /// <summary>
        /// Obliterates the multiverse history like a particularly careless time janitor.
        /// </summary>
        public void ClearSnapshots()
        {
            _snapshots.Clear();
        }

        public void RegisterTimelineAppendedHook(Action hook)
        {
            _onAppend = hook;
            lock (_syncRoot)
            {
                _onAppend = hook;
            }
        }

        public void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice)
        {
            lock (_syncRoot)
            {
                List<QuBit<T>> copy = [.. variable.Timeline.Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))];
                _snapshots.Push((variable, copy));
                QuantumLedgerOfRegret.Record(new TimelineAppendOperation<T>(variable, copy, newSlice));
                variable.NotifyFirstAppend();
                variable.AppendFromReverse(newSlice); // standardized append path (stamps epoch + event)
            }
        }

        public void RestoreLastSnapshot()
        {
            lock (_syncRoot)
            {
                (PositronicVariable<T> variable, List<QuBit<T>> oldTimeline) = _snapshots.Pop();
                // Replace forward history with bootstrap then append remaining slices
                if (oldTimeline.Count > 0)
                {
                    variable.ReplaceForwardHistoryWith(oldTimeline[0]);
                    for (int i = 1; i < oldTimeline.Count; i++)
                    {
                        variable.AppendFromReverse(oldTimeline[i]);
                    }
                }
            }
        }

        public void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice)
        {
            lock (_syncRoot)
            {
                List<QuBit<T>> backup = [.. variable.Timeline.Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))];
                QuantumLedgerOfRegret.Record(new TimelineReplaceOperation<T>(variable, backup));
                variable.ReplaceLastFromReverse(mergedSlice);
            }
        }

        public void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice)
        {
            lock (_syncRoot)
            {
                // Clear and set bootstrap via ReplaceForwardHistoryWith which truncates to bootstrap then appends
                variable.ReplaceForwardHistoryWith(slice);
            }
        }
    }
}
