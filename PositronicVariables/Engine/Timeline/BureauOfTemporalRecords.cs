using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine.Timeline
{
    public class BureauOfTemporalRecords<T> : ITimelineArchivist<T>
        where T : IComparable<T>
    {
        private readonly Stack<(PositronicVariable<T> Variable, List<QuBit<T>> Timeline)> _snapshots = new();
        private readonly object _syncRoot = new object();
        private Action _onAppend;

        /// <summary>
        /// Obliterates the multiverse history like a particularly careless time janitor.
        /// </summary>
        public void ClearSnapshots() => _snapshots.Clear();

        public void RegisterTimelineAppendedHook(Action hook)
        {
            _onAppend = hook;
            lock (_syncRoot)
                _onAppend = hook;
        }

        public void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice)
        {
            lock (_syncRoot)
            {
                // snapshot the current incarnation of the variable, before we accidentally turn it into a lizard or a toaster
                var copy = variable.timeline
                                   .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                                   .ToList();
                _snapshots.Push((variable, copy));

                // append the new qubit
                QuantumLedgerOfRegret.Record(new TimelineAppendOperation<T>(variable, copy, newSlice));

                // tell the variable its bootstrap has definitely gone
                variable.NotifyFirstAppend();

                // fire the hook so the convergence engine knows "something changed"
                variable.timeline.Add(newSlice);
                _onAppend?.Invoke();
            }
        }


        public void RestoreLastSnapshot()
        {
            lock (_syncRoot)
            {
                // summon the ghost of timelines past and cram it rudely back into existence
                var (variable, oldTimeline) = _snapshots.Pop();
                variable.timeline.Clear();
                variable.timeline.AddRange(oldTimeline);
            }
        }


        public void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice)
        {
            lock (_syncRoot)
            {
                var backup = variable.timeline
                                            .Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))
                                            .ToList();
                QuantumLedgerOfRegret.Record(new TimelineReplaceOperation<T>(variable, backup));

                variable.timeline[^1] = mergedSlice;
                _onAppend?.Invoke();
            }
        }

        public void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice)
        {
            lock (_syncRoot)
            {
                variable.timeline.Clear();
                variable.timeline.Add(slice);
                _onAppend?.Invoke();
            }
        }
    }
}
