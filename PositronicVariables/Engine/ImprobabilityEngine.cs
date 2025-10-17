using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;

namespace PositronicVariables.Engine
{
    // --- Convergence engine orchestrating the core loop ---
    public class ImprobabilityEngine<T> : IImprobabilityEngine<T>
        where T : IComparable<T>
    {
        private readonly IEntropyController _entropy;
        private readonly IOperationLogHandler<T> _ops;
        private readonly ISubEthaTransponder _redirect;
        private readonly IPositronicRuntime _runtime;
        private readonly ITimelineArchivist<T> _timelineArchivist;
        private readonly int _maxIters = 1000;

        public ImprobabilityEngine(
            IEntropyController entropy,
            IOperationLogHandler<T> ops,
            ISubEthaTransponder redirect,
            IPositronicRuntime runtime,
            ITimelineArchivist<T> timelineArchivist)
        {
            _entropy = entropy;
            _ops = ops;
            _redirect = redirect;
            _runtime = runtime;
            _timelineArchivist = timelineArchivist;
        }

        public void Run(Action code,
                        bool runFinalIteration = true,
                        bool unifyOnConvergence = true,
                        bool bailOnFirstReverseWhenIdle = false,
                        IImprobabilityEngine<T> next = null)
        {
            if (next != null)
            {
                // If part of a chain, call the next engine
                next.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
                return;
            }

            _redirect.Redirect();
            _entropy.Initialise();

            bool hadForwardCycle = false;
            bool skippedFirstForward = false;
            int iteration = 0;

            int hardLimit = unifyOnConvergence ? _maxIters : 2;

            while (!_runtime.Converged && iteration < hardLimit)
            {
                // Reset at the *start* of every half-cycle, not just forward ones,
                // so the engine doesn't confuse what has happened, what will happen,
                // and what it merely *thinks* has happened (which hasn't happened yet, probably).
                _ops.SawForwardWrite = false; // Why? Guarantees HadForwardAppend is in a known state; without this the engine occasionally thought "nothing happened" and broke the snapshot-clearing logic.

                // skip the *very* first forward cycle if requested
                // this is useful for scenarios where you want to
                // immediately start in reverse time, e.g. for
                // probing variable domains without any initial
                // assignments having been made
                // After all, it hasn't technically happened yet, and possibly never will, depending on who's asking
                if (!(bailOnFirstReverseWhenIdle
                    && _entropy.Entropy > 0
                    && hadForwardCycle
                    && !skippedFirstForward))
                {
                    // mark the start of this forward half-cycle so reverse replay
                    // can peel exactly one cycle's worth worth of operations
                    // peel-back is painfully precise; it must not overshoot or undershoot.
                    // just ask a slugblaster.
                    if (_entropy.Entropy > 0)
                    {
                        QuantumLedgerOfRegret.Record(new MerlinFroMarker());
                    }

                    code();
                }
                else
                {
                    skippedFirstForward = true;
                }

                if (_entropy.Entropy > 0)
                {
                    hadForwardCycle = true;
                }

                if (bailOnFirstReverseWhenIdle
                    && _entropy.Entropy < 0
                    && !_ops.SawForwardWrite)
                {
                    _runtime.Converged = false;
                    break;
                }

                if (hadForwardCycle && PositronicVariable<T>.AllConverged(_runtime) && _entropy.Entropy < 0)
                {
                    _timelineArchivist.ClearSnapshots();

                    if (unifyOnConvergence)
                    {
                        foreach (IPositronicVariable pv in PositronicVariable<T>.GetAllVariables(_runtime))
                        {
                            pv.UnifyAll();
                        }
                    }

                    _runtime.Converged = true;
                    break;
                }

                _entropy.Flip();
                // Reality is extremly fragile, each PositronicVariable<T> has a timeline
                // a list of QuBit<T> slices representing its different states across forward and reverse iterations.
                // When the convergence engine flips entropy, it effectively rewinds or fast-forwards time.
                // Now if we don't clone the last slice as we do here, both the current timeline entry and the incoming value
                // would reference the same QuBit<T> object causing a catastrophic collapse of the multiverse.
                // When journeying backwards through time, one should never share quantum state with oneself.
                // It's like borrowing your toothbrush from a parallel universe—things get weird very fast.
                if (_entropy.Entropy < 0)
                {
                    foreach (IPositronicVariable v in PositronicVariable<T>.GetAllVariables(_runtime))
                    {
                        PositronicVariable<T> pv = (PositronicVariable<T>)v;
                        QuBit<T> last = pv.GetCurrentQBit();
                        QuBit<T> copy = new(last.ToCollapsedValues().ToArray());
                        _ = copy.Any();
                        pv.Assign(copy);
                    }
                }

                iteration++;
            }

            if (runFinalIteration && _runtime.Converged)
            {
                // We give the universe one last chance to tidy itself up before the auditors arrive.
                if (unifyOnConvergence)
                {
                    _timelineArchivist.ClearSnapshots();
                    foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(_runtime).OfType<PositronicVariable<T>>())
                    {
                        v.UnifyAll();
                    }
                }
                // This is the quantum equivalent of nodding politely after the universe finishes talking.
                _runtime.Entropy = 1;
                _runtime.Converged = false;

                // undo any reversible ops from that final pass
                // the cosmic broom sweeps up the paradoxes before they stain the carpet.
                _ops.UndoLastForwardCycle();

                // Trim the timelines, purge any stray Loki variants.
                _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();

                code();

                _runtime.Converged = true;
            }
            else if (runFinalIteration && !_runtime.Converged && unifyOnConvergence)
            {
                // Fallback: we didn't hit the "converged on reverse" condition,
                // but we still want console-style programs to print the unified result.
                _timelineArchivist.ClearSnapshots();
                foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(_runtime)
                                                       .OfType<PositronicVariable<T>>())
                {
                    v.UnifyAll();
                }

                _runtime.Entropy = 1;
                _runtime.Converged = false;
                _ops.UndoLastForwardCycle();
                _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
                code();                     // one last forward pass purely to emit output
                _runtime.Converged = true;
            }

            _ops.Clear();

            _timelineArchivist.ClearSnapshots();
            // re-align each universe's timeline so the current state is the only state
            foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(_runtime)
                                                   .OfType<PositronicVariable<T>>())
            {
                v.ResetDomainToCurrent();

                if (unifyOnConvergence && v.timeline.Count > 1)
                {
                    v.UnifyAll();   // Every alternate reality now agrees to disagree quietly.
                }
            }

            // This is the portal home back to our reference universe.
            _redirect.Restore();
        }
    }
}
