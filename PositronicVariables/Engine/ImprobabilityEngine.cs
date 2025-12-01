using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Linq;
using System.Collections.Generic;
using PositronicVariables.Engine.Coordinator;
using PositronicVariables.Transactions;

namespace PositronicVariables.Engine
{
    // --- Convergence engine orchestrating the core loop ---
    public class ImprobabilityEngine<T>(
        IEntropyController entropy,
        IOperationLogHandler<T> ops,
        ISubEthaTransponder redirect,
        IPositronicRuntime runtime,
        ITimelineArchivist<T> timelineArchivist,
        ConvergenceCoordinator coordinator = null) : IImprobabilityEngine<T>
        where T : IComparable<T>
    {
        private readonly int _maxIters = 1000;

        // Expose runtime for nested work item
        internal IPositronicRuntime Runtime => runtime;

        private sealed class ConvergenceWorkItem : IConvergenceWorkItem
        {
            private readonly ImprobabilityEngine<T> _engine;
            private readonly Action _code;
            private readonly bool _runFinalIteration;
            private readonly bool _unify;
            private readonly bool _bail;
            private readonly IImprobabilityEngine<T> _next;
            public ConvergenceWorkItem(ImprobabilityEngine<T> engine, Action code, bool runFinal, bool unify, bool bail, IImprobabilityEngine<T> next)
            {
                _engine = engine; _code = code; _runFinalIteration = runFinal; _unify = unify; _bail = bail; _next = next;
            }
            public void BuildWrites(TransactionV2 tx)
            {
                // Run convergence logic which will call into ITimelineArchivist.
                // Archivist detects active tx and stages writes into 'tx' via TransactionV2.Current.
                _engine.RunInternal(_code, _runFinalIteration, _unify, _bail, _next);

                // No direct staging here; archivist handles StageWrite per mutation site.
            }
            public IEnumerable<Action> BuildCommitHooks() { yield break; }
            public object? GetResultAfterCommit() => null;
        }

        public void Run(Action code,
                        bool runFinalIteration = true,
                        bool unifyOnConvergence = true,
                        bool bailOnFirstReverseWhenIdle = false,
                        IImprobabilityEngine<T> next = null)
        {
            if (coordinator != null)
            {
                coordinator.EnqueueAsync(new ConvergenceWorkItem(this, code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle, next)).AsTask().GetAwaiter().GetResult();
                return;
            }
            RunInternal(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle, next);
        }

        private void RunInternal(Action code,
                        bool runFinalIteration,
                        bool unifyOnConvergence,
                        bool bailOnFirstReverseWhenIdle,
                        IImprobabilityEngine<T> next)
        {
            if (next != null)
            {
                // If part of a chain, call the next engine
                next.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
                return;
            }

            redirect.Redirect();
            entropy.Initialise();

            bool hadForwardCycle = false;
            bool skippedFirstForward = false;
            int iteration = 0;

            int hardLimit = unifyOnConvergence ? _maxIters : 2;

            while (!Runtime.Converged && iteration < hardLimit)
            {
                ops.SawForwardWrite = false;

                if (!(bailOnFirstReverseWhenIdle
                    && entropy.Entropy > 0
                    && hadForwardCycle
                    && !skippedFirstForward))
                {
                    if (entropy.Entropy > 0)
                    {
                        QuantumLedgerOfRegret.Record(new MerlinFroMarker());
                    }

                    code();
                }
                else
                {
                    skippedFirstForward = true;
                }

                if (entropy.Entropy > 0)
                {
                    hadForwardCycle = true;
                }

                if (bailOnFirstReverseWhenIdle
                    && entropy.Entropy < 0
                    && !ops.SawForwardWrite)
                {
                    Runtime.Converged = false;
                    break;
                }

                if (hadForwardCycle && PositronicVariable<T>.AllConverged(Runtime) && entropy.Entropy < 0)
                {
                    timelineArchivist.ClearSnapshots();

                    if (unifyOnConvergence)
                    {
                        foreach (IPositronicVariable pv in PositronicVariable<T>.GetAllVariables(Runtime))
                        {
                            pv.UnifyAll();
                        }
                    }

                    Runtime.Converged = true;
                    break;
                }

                entropy.Flip();
                if (entropy.Entropy < 0)
                {
                    foreach (IPositronicVariable v in PositronicVariable<T>.GetAllVariables(Runtime))
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

            if (runFinalIteration && Runtime.Converged)
            {
                if (unifyOnConvergence)
                {
                    timelineArchivist.ClearSnapshots();
                    foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(Runtime).OfType<PositronicVariable<T>>())
                    {
                        v.UnifyAll();
                    }
                }
                Runtime.Entropy = 1;
                Runtime.Converged = false;
                ops.UndoLastForwardCycle();
                _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
                code();
                Runtime.Converged = true;
            }
            else if (runFinalIteration && !Runtime.Converged && unifyOnConvergence)
            {
                timelineArchivist.ClearSnapshots();
                foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(Runtime)
                                                       .OfType<PositronicVariable<T>>())
                {
                    v.UnifyAll();
                }

                Runtime.Entropy = 1;
                Runtime.Converged = false;
                ops.UndoLastForwardCycle();
                _ = AethericRedirectionGrid.ImprobabilityDrive.GetStringBuilder().Clear();
                code();
                Runtime.Converged = true;
            }

            ops.Clear();

            timelineArchivist.ClearSnapshots();
            foreach (PositronicVariable<T> v in PositronicVariable<T>.GetAllVariables(Runtime)
                                                   .OfType<PositronicVariable<T>>())
            {
                v.ResetDomainToCurrent();

                if (unifyOnConvergence && v.Timeline.Count > 1)
                {
                    v.UnifyAll();
                }
            }

            redirect.Restore();
        }
    }
}
