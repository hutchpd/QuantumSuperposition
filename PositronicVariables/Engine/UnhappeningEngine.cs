using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Operations;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PositronicVariables.Engine
{
    public class UnhappeningEngine<T>(IOperationLogHandler<T> ops, ITimelineArchivist<T> versioningService)
        where T : IComparable<T>
    {
        private readonly IOperationLogHandler<T> _ops = ops;
        private readonly ITimelineArchivist<T> _versioningService = versioningService;

        /// <summary>
        /// Peel off the last forward half‑cycle, and rebuild every possible earlier state
        /// that would make the *later* writes self‑consistent.
        ///
        /// Semantics: we move *backwards in execution order*, but for each operation
        ///    we apply its **forward** mapping. Example:
        ///       x = 10;            // last write
        ///       x = x + 1;         // earlier in code
        ///    The earlier read must be 11 (addition still "adds" as we walk back).
        ///
        /// The only special case is modulus: the forward mapping we apply is the
        /// "rebuild" q·d + r using the quotient captured at record‑time.
        /// </summary>
        public QuBit<T> ReplayReverseCycle(QuBit<T> incoming, PositronicVariable<T> variable)
        {
            // Global peel newest→oldest; scope decisions to 'variable'.
            List<IOperation> poppedSnapshots = [];
            List<IReversibleOperation<T>> poppedReversibles = [];

            // Per-variable bookkeeping
            HashSet<T> forwardValuesForThisVar = [];
            List<T> preReplaceScalarAppendsForThisVar = [];

            // IMPORTANT: this flips when we hit the FIRST snapshot (append or replace) for THIS variable.
            // From that point backward we collect reversible ops for THIS variable only.
            bool encounteredClosingSnapshotForThisVar = false;

            while (true)
            {
                IOperation top = QuantumLedgerOfRegret.Peek();
                switch (top)
                {
                    case null:
                        goto DonePeel;

                    case MerlinFroMarker:
                        goto DonePeel;

                    case TimelineAppendOperation<T> tap:
                        {
                            poppedSnapshots.Add(tap);

                            if (BelongsToVariable(tap, variable))
                            {
                                IEnumerable<T> vals = tap.AddedSlice.ToCollapsedValues();

                                if (!encounteredClosingSnapshotForThisVar)
                                {
                                    // Appends more recent than the closing snapshot survive toward the print site.
                                    forwardValuesForThisVar.UnionWith(vals);
                                    // The first snapshot we meet for this var is its closing write (scalar-append close case).
                                    encounteredClosingSnapshotForThisVar = true;
                                }
                                else
                                {
                                    // Older-than-close appends were overwritten by the closing write.
                                    preReplaceScalarAppendsForThisVar.AddRange(vals);
                                }
                            }

                            QuantumLedgerOfRegret.Pop();
                            continue;
                        }

                    case TimelineReplaceOperation<T> trp:
                        {
                            poppedSnapshots.Add(trp);

                            if (BelongsToVariable(trp, variable) && !encounteredClosingSnapshotForThisVar)
                            {
                                // Replace is the closing write for this var.
                                encounteredClosingSnapshotForThisVar = true;
                            }

                            QuantumLedgerOfRegret.Pop();
                            continue;
                        }

                    case IReversibleOperation<T> rop:
                        {
                            // Only collect reversibles for THIS variable after we crossed its closing snapshot.
                            if (BelongsToVariable(rop, variable) && encounteredClosingSnapshotForThisVar)
                            {
                                poppedReversibles.Add(rop);
                            }

                            // Pop globally as before.
                            QuantumLedgerOfRegret.Pop();
                            continue;
                        }

                    default:
                        goto DonePeel;
                }
            }

        DonePeel:

            // Undo all popped snapshots (global behavior preserved)
            foreach (IOperation op in poppedSnapshots.AsEnumerable().Reverse())
            {
                op.Undo();
            }

            // Compute “scalar close” strictly for THIS variable
            IEnumerable<T> incomingVals = incoming.ToCollapsedValues();

            bool hasArithmeticForThisVar = poppedReversibles.Count > 0;

            bool sawReplaceForThisVar = poppedSnapshots
                .OfType<TimelineReplaceOperation<T>>()
                .Any(op => BelongsToVariable(op, variable));

            bool lastAppendWasScalarForThisVar =
                poppedSnapshots.AsEnumerable().Reverse()
                    .Where(op => op is TimelineAppendOperation<T> a && BelongsToVariable(a, variable))
                    .Cast<TimelineAppendOperation<T>>()
                    .Select(a => a.AddedSlice.ToCollapsedValues().Count() == 1)
                    .FirstOrDefault(false);

            bool scalarWriteDetected =
                hasArithmeticForThisVar && (sawReplaceForThisVar || lastAppendWasScalarForThisVar);

            bool includeForward = poppedReversibles.Count == 0 || !scalarWriteDetected;

            IEnumerable<T> seeds;
            IEnumerable<T> bootstrap = variable.timeline[0].ToCollapsedValues();

            if (!scalarWriteDetected)
            {
                // In degenerate no-arithmetic case, drop bootstrap to avoid stale-union bleed.
                bool excludeBootstrap = !hasArithmeticForThisVar && bootstrap.Count() == 1;

                IEnumerable<T> fwd = excludeBootstrap ? forwardValuesForThisVar.Except(bootstrap) : forwardValuesForThisVar;
                IEnumerable<T> inc = excludeBootstrap ? incomingVals.Except(bootstrap) : incomingVals;

                seeds = includeForward ? fwd.Union(inc).Distinct() : inc;
            }
            else
            {
                // Scalar-close: rebuild from incoming; exclude pre-close history for this var
                IEnumerable<T> replacedForThisVar = poppedSnapshots
                    .OfType<TimelineReplaceOperation<T>>()
                    .Where(op => BelongsToVariable(op, variable))
                    .SelectMany(op => op.ReplacedSlice.ToCollapsedValues());

                bool noForwardAppendsForThisVar = !poppedSnapshots
                    .OfType<TimelineAppendOperation<T>>()
                    .Any(op => BelongsToVariable(op, variable));

                seeds = noForwardAppendsForThisVar && hasArithmeticForThisVar
                    ? incomingVals.Union(replacedForThisVar).Distinct()
                    : incomingVals
                        .Except(bootstrap)
                        .Except(replacedForThisVar)
                        .Distinct();
            }

            if (!seeds.Any())
            {
                seeds = incomingVals;
            }

            HashSet<T> rebuiltSet = [];

            if (poppedSnapshots.Count == 0 && poppedReversibles.Count == 0)
            {
                rebuiltSet.UnionWith(incomingVals);
            }

            // Special-case: (+k) then %d → residue class
            AdditionOperation<T> addOp = poppedReversibles.OfType<AdditionOperation<T>>().FirstOrDefault();
            ReversibleModulusOp<T> modOp = poppedReversibles.OfType<ReversibleModulusOp<T>>().FirstOrDefault();
            bool usedResidueClass = addOp is not null && modOp is not null;

            if (usedResidueClass)
            {
                int d = Convert.ToInt32(modOp.Divisor);
                for (int i = 0; i < d; i++)
                {
                    _ = rebuiltSet.Add((T)Convert.ChangeType(i, typeof(T)));
                }
            }
            else
            {
                foreach (T seed in seeds)
                {
                    T v = seed;
                    for (int i = 0; i < poppedReversibles.Count; i++)
                    {
                        v = poppedReversibles[i].ApplyForward(v);
                    }

                    _ = rebuiltSet.Add(v);
                }
            }

            if (!scalarWriteDetected && !hasArithmeticForThisVar)
            {
                bool excludeBootstrap = bootstrap.Count() == 1;
                rebuiltSet.UnionWith(excludeBootstrap ? forwardValuesForThisVar.Except(bootstrap) : forwardValuesForThisVar);
            }

            // Carefully wrap this absurd collection of maybe-states into one neat, Schrödinger-approved burrito
            QuBit<T> rebuilt = new(rebuiltSet.OrderBy(x => x).ToArray());
            _ = rebuilt.Any();

            // Append vs replace for this variable’s timeline growth
            bool anyAppendsForThisVar = poppedSnapshots
                .OfType<TimelineAppendOperation<T>>()
                .Any(op => BelongsToVariable(op, variable));

            if (scalarWriteDetected)
            {
                if (variable.timeline.Count > 1)
                {
                    variable.ReplaceForwardHistoryWith(rebuilt);
                }
                else
                {
                    variable.AppendFromReverse(rebuilt);
                }
            }
            else
            {
                if (!anyAppendsForThisVar && variable.timeline.Count > 0)
                {
                    variable.ReplaceLastFromReverse(rebuilt);
                }
                else
                {
                    variable.AppendFromReverse(rebuilt);
                }
            }


            return rebuilt;
        }

        // Reflect to determine if an operation targets the given variable instance
        private static bool BelongsToVariable(object entry, PositronicVariable<T> variable)
        {
            FieldInfo[] fields = entry.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                object val = f.GetValue(entry);
                if (ReferenceEquals(val, variable))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
