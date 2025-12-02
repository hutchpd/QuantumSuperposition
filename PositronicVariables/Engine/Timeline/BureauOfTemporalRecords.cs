using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PositronicVariables.Transactions;

namespace PositronicVariables.Engine.Timeline
{
    public class BureauOfTemporalRecords<T> : ITimelineArchivist<T>
        where T : IComparable<T>
    {
        private readonly Stack<(PositronicVariable<T> Variable, List<QuBit<T>> Timeline)> _snapshots = new();
        private readonly object _syncRoot = new();
        private Action _onAppend;
        private readonly ConcurrentDictionary<long, List<ImmutableTimelineSnapshot<T>>> _archive = new();

        /// <summary>
        /// Obliterates the multiverse history like a particularly careless time janitor.
        /// </summary>
        public void ClearSnapshots() => _snapshots.Clear();
        public void RegisterTimelineAppendedHook(Action hook) { _onAppend = hook; lock (_syncRoot) { _onAppend = hook; } }

        public void SnapshotAppend(PositronicVariable<T> variable, QuBit<T> newSlice)
        {
            var tx = TransactionV2.Current;
            if (tx != null && !tx.IsApplying && !PositronicVariable<T>.InConvergenceLoop)
            {
                tx.StageWrite(variable, newSlice, TxMutationKind.Append);
                // Schedule archival after commit
                tx.BufferLedgerEntry(new TimelineAppendOperation<T>(variable, CloneTimeline(variable), newSlice));
                tx.AddCommitHook(() => PublishSnapshot(BuildSnapshot(variable)));
                return;
            }
            lock (_syncRoot)
            {
                List<QuBit<T>> copy = CloneTimeline(variable);
                _snapshots.Push((variable, copy));
                RegretScribe<T>.Sink.Append(new TimelineAppendOperation<T>(variable, copy, newSlice), Guid.NewGuid());
                variable.NotifyFirstAppend();
                variable.AppendFromReverse(newSlice); // standardized append path (stamps epoch + event)
                PublishSnapshot(BuildSnapshot(variable));
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
                        variable.AppendFromReverse(oldTimeline[i]);
                    PublishSnapshot(BuildSnapshot(variable));
                }
            }
        }

        public void ReplaceLastSlice(PositronicVariable<T> variable, QuBit<T> mergedSlice)
        {
            var tx = TransactionV2.Current;
            if (tx != null && !tx.IsApplying && !PositronicVariable<T>.InConvergenceLoop)
            {
                tx.StageWrite(variable, mergedSlice, TxMutationKind.ReplaceLast);
                tx.BufferLedgerEntry(new TimelineReplaceOperation<T>(variable, CloneTimeline(variable)));
                tx.AddCommitHook(() => PublishSnapshot(BuildSnapshot(variable)));
                return;
            }
            lock (_syncRoot)
            {
                List<QuBit<T>> backup = CloneTimeline(variable);
                RegretScribe<T>.Sink.Append(new TimelineReplaceOperation<T>(variable, backup), Guid.NewGuid());
                variable.ReplaceLastFromReverse(mergedSlice);
                PublishSnapshot(BuildSnapshot(variable));
            }
        }

        public void OverwriteBootstrap(PositronicVariable<T> variable, QuBit<T> slice)
        {
            var tx = TransactionV2.Current;
            if (tx != null && !tx.IsApplying && !PositronicVariable<T>.InConvergenceLoop)
            {
                tx.StageWrite(variable, slice, TxMutationKind.OverwriteBootstrap);
                tx.AddCommitHook(() => PublishSnapshot(BuildSnapshot(variable)));
                return;
            }
            lock (_syncRoot)
            {
                // Clear and set bootstrap via ReplaceForwardHistoryWith which truncates to bootstrap then appends
                variable.ReplaceForwardHistoryWith(slice);
                PublishSnapshot(BuildSnapshot(variable));
            }
        }

        public void PublishSnapshot(ImmutableTimelineSnapshot<T> snapshot)
        {
            if (snapshot == null) return;
            List<ImmutableTimelineSnapshot<T>> list = _archive.GetOrAdd(snapshot.PositronicVariableId, _ => new List<ImmutableTimelineSnapshot<T>>());
            lock (list)
            {
                list.Add(snapshot);
            }
        }

        public IReadOnlyList<ImmutableTimelineSnapshot<T>> GetSnapshots(long variableId)
        {
            if (_archive.TryGetValue(variableId, out var list))
            {
                lock (list)
                {
                    return list.ToList();
                }
            }
            return Array.Empty<ImmutableTimelineSnapshot<T>>();
        }

        private static List<QuBit<T>> CloneTimeline(PositronicVariable<T> variable)
        {
            return [.. variable.Timeline.Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray()))];
        }

        private static ImmutableTimelineSnapshot<T> BuildSnapshot(PositronicVariable<T> variable)
        {
            long id = ((ITransactionalVariable)variable).TxId;
            long ver = ((ITransactionalVariable)variable).TxVersion;
            QuBit<T>[] slices = variable.Timeline.Select(q => new QuBit<T>(q.ToCollapsedValues().ToArray())).ToArray();
            foreach (var s in slices) { s.Any(); }
            return new ImmutableTimelineSnapshot<T>(id, ver, slices);
        }
    }
}
