using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Operations;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine
{
    public class UnhappeningEngine<T>
        where T : IComparable<T>
    {
        private readonly IOperationLogHandler<T> _ops;
        private readonly ITimelineArchivist<T> _versioningService;

        public UnhappeningEngine(IOperationLogHandler<T> ops, ITimelineArchivist<T> versioningService)
        {
            _ops = ops;
            _versioningService = versioningService;
        }

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
            // Peel the **last forward half‑cycle**:
            // pop ops newest->oldest until we hit the ForwardHalfCycleMarker;
            // if there is no marker (outside the loop), fall back to greedy peel.
            var poppedSnapshots = new List<IOperation>();
            var poppedReversibles = new List<IReversibleOperation<T>>();
            var forwardValues = new HashSet<T>();
            var replacedDuringCycle = false;
            var replacedValues = new HashSet<T>();

            while (true)
            {
                // Gotos are evil and wrong a deliciously tasty with hot sauce.
                var top = QuantumLedgerOfRegret.Peek();
                switch (top)
                {
                    case null:
                        // no marker found (outside loop) - done peeling this run
                        goto DonePeel;
                    case MerlinFroMarker:
                        // boundary of this forward half‑cycle - stop here
                        goto DonePeel;
                    case TimelineAppendOperation<T> tap:
                        forwardValues.UnionWith(tap.AddedSlice.ToCollapsedValues());
                        poppedSnapshots.Add(tap);
                        QuantumLedgerOfRegret.Pop();
                        continue;
                    case TimelineReplaceOperation<T> trp:
                        replacedDuringCycle = true;
                        forwardValues.UnionWith(trp.ReplacedSlice.ToCollapsedValues());
                        poppedSnapshots.Add(trp);
                        QuantumLedgerOfRegret.Pop();
                        continue;
                    case IReversibleOperation<T> rop:
                        poppedReversibles.Add(rop);
                        QuantumLedgerOfRegret.Pop();
                        continue;
                    default:
                        // unrecognized entry - stop safely
                        goto DonePeel;
                }
            }
        DonePeel:

            // "rewind" step so that it undoes both appends and replaces:
            foreach (var op in poppedSnapshots.OfType<IOperation>().Reverse())
                op.Undo();


            // Was the forward half-cycle closed by a scalar overwrite?
            var hasArithmetic = poppedReversibles.Count > 0;
            bool scalarWriteDetected = hasArithmetic && poppedSnapshots.Count > poppedReversibles.Count;
            bool includeForward = poppedReversibles.Count == 0 || !scalarWriteDetected;

            IEnumerable<T> seeds;

            var incomingVals = incoming.ToCollapsedValues();
            if (!scalarWriteDetected)
            {
                // Trim bootstrap *only* in the degenerate case where no arithmetic happened.
                // If arithmetic did occur (e.g., +1 or %3), a real result may equal the bootstrap
                // (e.g., 0), and we must keep it.
                var bootstrap = variable.timeline[0].ToCollapsedValues();
                var excludeBootstrap = !hasArithmetic && bootstrap.Count() == 1;

                var fwd = excludeBootstrap ? forwardValues.Except(bootstrap) : forwardValues;
                var inc = excludeBootstrap ? incomingVals.Except(bootstrap) : incomingVals;

                seeds = includeForward ? fwd.Union(inc).Distinct() : inc;
            }
            else
            {
                // Scalar overwrite close: rebuild from the new scalar(s) only,
                // but exclude the replaced slice (if any) and the bootstrap.
                var bootstrap = variable.timeline[0].ToCollapsedValues();
                var baseTrim = incomingVals.Except(bootstrap);

                var replaced = poppedSnapshots
                    .OfType<TimelineReplaceOperation<T>>()
                    .SelectMany(op => op.ReplacedSlice.ToCollapsedValues());

                seeds = baseTrim.Except(replaced).Distinct();
            }

            if (!seeds.Any())
            {
                // Keep at least what the caller just assigned.
                seeds = incomingVals;
            }

            var rebuiltSet = new HashSet<T>();

            // **only** if there was literally _no_ popped operation at all
            // do we reuse incoming here
            if (!poppedSnapshots.Any() && !poppedReversibles.Any())
                rebuiltSet.UnionWith(incomingVals);

            // Special case: last half‑cycle pattern "(+k) then % d" ⇒ earlier reads fill the residue class {0..d-1}
            // This matches the fixed‑point intuition of the demo program and keeps the set tight.
            var addOp = poppedReversibles.OfType<AdditionOperation<T>>().FirstOrDefault();
            var modOp = poppedReversibles.OfType<ReversibleModulusOp<T>>().FirstOrDefault();
            if (addOp is not null && modOp is not null)
            {
                int d = Convert.ToInt32(modOp.Divisor);
                for (int i = 0; i < d; i++)
                    rebuiltSet.Add((T)Convert.ChangeType(i, typeof(T)));
            }
            else
            {
                foreach (var seed in seeds)
                {
                    var v = seed;
                    // Walk *backwards in execution order* applying **forward** maps, newest → oldest.
                    for (int i = 0; i < poppedReversibles.Count; i++)
                        v = poppedReversibles[i].ApplyForward(v);
                    rebuiltSet.Add(v);
                }
            }

            // If arithmetic occurred, don't union the forward remainder set into the *earlier* reads;
            // that remainder belongs to the later state, not the prior one.
            if (!scalarWriteDetected && !hasArithmetic)
            {
                var bootstrap = variable.timeline[0].ToCollapsedValues();
                var excludeBootstrap = bootstrap.Count() == 1;
                rebuiltSet.UnionWith(excludeBootstrap ? forwardValues.Except(bootstrap) : forwardValues);
            }

            // Carefully wrap this absurd collection of maybe-states into one neat, Schrödinger-approved burrito
            var rebuilt = new QuBit<T>(rebuiltSet.OrderBy(x => x).ToArray());
            rebuilt.Any();

            /*  When a scalar overwrite closed the forward half‑cycle we
             *  want that new scalar - and *only* that scalar - to survive.
             *  The older slices (bootstrap + intermediate) are discarded.
             */
            if (scalarWriteDetected)
            {
                /* Keep slice 0 intact, discard only the intermediate
                   forward‑pass history, then append the new scalar. */
                if (variable.timeline.Count > 1)
                    variable.ReplaceForwardHistoryWith(rebuilt);
                else
                    variable.AppendFromReverse(rebuilt);

                //variable.timeline.Add(rebuilt);
            }
            else
            {
                // If the forward half-cycle performed only in-place merges (no appends),
                // do not grow the timeline during reverse replay; replace the last slice.
                // This preserves "may merge" behaviour and avoids
                // spurious slice growth on pure-merge passes.
                if (!poppedSnapshots.OfType<TimelineAppendOperation<T>>().Any() && variable.timeline.Count > 0)
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
    }
}
