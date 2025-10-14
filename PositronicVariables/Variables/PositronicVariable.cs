using Microsoft.Extensions.Hosting;
using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Engine;
using PositronicVariables.Initialisation;
using PositronicVariables.Operations;
using PositronicVariables.Runtime;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PositronicVariables.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace PositronicVariables.Variables
{
    /// <summary>
    /// A "positronic" variable that stores a timeline of QuBit&lt;T&gt; states.
    /// Supports negative-time convergence, partial unification, etc.
    /// </summary>
    /// <typeparam name="T">A type that implements IComparable.</typeparam>
    public class PositronicVariable<T> : IPositronicVariable
        where T : IComparable<T>
    {
        // Determines at runtime if T is a value type.
        private readonly bool isValueType = typeof(T).IsValueType;
        private readonly HashSet<T> _domain = new();
        private static bool _reverseReplayStarted;
        public static int _loopDepth;
        private readonly IPositronicRuntime _runtime;
        // --- Epoch tagging ---
        // - sliceEpochs is kept strictly in lockstep with 'timeline'
        // -  0  : bootstrap
        // - -1  : outside-loop writes
        // - 1+  : each invocation of RunConvergenceLoop increments this per-T epoch
        private static int s_CurrentEpoch = 0;
        private readonly List<int> _sliceEpochs = new();
        private bool _hasWrittenInitialForward = false;
        internal void NotifyFirstAppend() => bootstrapSuperseded = true;

        internal static bool InConvergenceLoop => _loopDepth > 0;
        private readonly UnhappeningEngine<T> _reverseReplay;
        private readonly IOperationLogHandler<T> _ops;

        private readonly ITimelineArchivist<T> _temporalRecords;

        private static readonly object s_initLock = new();
        private bool _hadOutsideWritesSinceLastLoop = false;
        private static IPositronicRuntime EnsureAmbientRuntime()
        {
            if (!PositronicAmbient.IsInitialized)
            {
                lock (s_initLock)
                {
                    if (!PositronicAmbient.IsInitialized)
                    {
                        // Build a minimal host and register the default runtime
                        var hb = Host.CreateDefaultBuilder()
                                     .ConfigureServices(s => s.AddPositronicRuntime());
                        PositronicAmbient.InitialiseWith(hb);
                    }
                }
            }
            return PositronicAmbient.Current;
        }

        public static PositronicVariable<T> GetOrCreate(string id, T initialValue)
            => GetOrCreate(id, initialValue, EnsureAmbientRuntime());

        public static PositronicVariable<T> GetOrCreate(string id)
            => GetOrCreate(id, EnsureAmbientRuntime());

        public static PositronicVariable<T> GetOrCreate(T initialValue)
            => GetOrCreate(initialValue, EnsureAmbientRuntime());

        public static PositronicVariable<T> GetOrCreate()
            => GetOrCreate(EnsureAmbientRuntime());

        public void SeedBootstrap(params T[] values)
        {
            var qb = new QuBit<T>(values);
            qb.Any();
            _temporalRecords.OverwriteBootstrap(this, qb); // A small sacrifice to the versioning gods
            _sliceEpochs.Clear();
            _sliceEpochs.Add(0); // bootstrap epoch == 0
            bootstrapSuperseded = false;
        }




        public PositronicVariable(
            T initialValue,
            IPositronicRuntime runtime,
            ITimelineArchivist<T> timelineArchivist = null,
            IOperationLogHandler<T> opsHandler = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

            // seed the timeline exactly once
            _temporalRecords = timelineArchivist ?? new BureauOfTemporalRecords<T>();
            var qb = new QuBit<T>(new[] { initialValue });
            qb.Any();
            _temporalRecords.OverwriteBootstrap(this, qb);
            _sliceEpochs.Clear();
            _sliceEpochs.Add(0); // bootstrap
            _hasWrittenInitialForward = true;

            _domain.Add(initialValue);
            _runtime.Registry.Add(this);

            // DI my temporal records and ops handler
            _ops = opsHandler ?? new RegretScribe<T>();
            _reverseReplay = new UnhappeningEngine<T>(_ops, _temporalRecords);

            // "something appended" hook
            _temporalRecords.RegisterTimelineAppendedHook(() => OnTimelineAppended?.Invoke());
        }

        static PositronicVariable()
        {
            // Do the old IComparable guard
            if (!(typeof(IComparable).IsAssignableFrom(typeof(T)) ||
                  typeof(IComparable<T>).IsAssignableFrom(typeof(T))))
            {
                throw new InvalidOperationException(
                    $"Type parameter '{typeof(T).Name}' for PositronicVariable " +
                    "must implement IComparable or IComparable<T> to enable proper convergence checking.");
            }

            // Trigger the neuro-cascade init, etc.
            var _ = AethericRedirectionGrid.Initialised;
        }

        private void Remember(IEnumerable<T> xs)
        {
            foreach (var x in xs) _domain.Add(x);
        }

        internal static void ResetReverseReplayFlag()
        {
            _reverseReplayStarted = false;           // called each time the direction flips
        }

        /// <summary>
        /// Secret lever that fires whenever a new timeline twig is grafted on
        /// </summary>
        internal static Action TimelineAppendedHook;

        /// <summary>
        /// Injects an explicit QuBit into the timeline **without collapsing it**.
        /// </summary>
        public void Assign(QuBit<T> qb)
        {
            if (qb is null)
                throw new ArgumentNullException(nameof(qb));

            // Ensure it's been observed at least once so that
            // subsequent operator overloads won't collapse accidentally.
            qb.Any();
            ReplaceOrAppendOrUnify(qb, replace: true);
        }

        // Automatically enable simulation on first use.
        private static readonly bool _ = EnableSimulation();
        private static bool EnableSimulation()
        {
            NeuroCascadeInitialiser.AutoEnable();
            return true;
        }



        // (removed) static registry in favor of instance‐based IPositronicVariableRegistry
        //private static Dictionary<string, PositronicVariable<T>> registry = new Dictionary<string, PositronicVariable<T>>();

        // The timeline of quantum slices.
        public readonly List<QuBit<T>> timeline = new List<QuBit<T>>();
        private bool bootstrapSuperseded = false;

        // Epoch helpers
        internal static void BeginEpoch() => s_CurrentEpoch++;   // called once per RunConvergenceLoop
        internal static int CurrentEpoch => s_CurrentEpoch;
        private static int OutsideEpoch => -1;

        internal void StampBootstrap()
        {
            _sliceEpochs.Clear();
            _sliceEpochs.Add(0);
        }
        internal void StampAppendCurrentEpoch()
        {
            _sliceEpochs.Add(InConvergenceLoop ? CurrentEpoch : OutsideEpoch);
        }
        internal void StampReplaceCurrentEpoch()
        {
            if (_sliceEpochs.Count == 0) _sliceEpochs.Add(0);
            _sliceEpochs[^1] = InConvergenceLoop ? CurrentEpoch : OutsideEpoch;
        }
        internal void TruncateToBootstrapOnly()
        {
            if (_sliceEpochs.Count > 1)
                _sliceEpochs.RemoveRange(1, _sliceEpochs.Count - 1);
        }

        public event Action OnConverged;
        public event Action OnCollapse;
        public event Action OnTimelineAppended;

        /// <summary>
        /// How many alternate realities we've stacked on this poor variable's timeline. More is usually bad.
        /// </summary>
        public int TimelineLength => timeline.Count;

        /// <summary>
        /// Sets the global entropy (direction of time) for the given runtime.
        /// </summary>
        /// <param name="rt">The current runtime</param>
        /// <param name="e">Entropy direction -1 for backwards, 1 for forwards.</param>
        public static void SetEntropy(IPositronicRuntime rt, int e) => rt.Entropy = e;
        /// <summary>
        /// Gets the global entropy (direction of time) for the given runtime.
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public static int GetEntropy(IPositronicRuntime rt) => rt.Entropy;


        public static PositronicVariable<T> GetOrCreate(string id, T initialValue, IPositronicRuntime runtime)
            => runtime.Factory.GetOrCreate<T>(id, initialValue);


        public static PositronicVariable<T> GetOrCreate(string id, IPositronicRuntime runtime)
            => runtime.Factory.GetOrCreate<T>(id);


        public static PositronicVariable<T> GetOrCreate(T initialValue, IPositronicRuntime runtime)
            => runtime.Factory.GetOrCreate<T>(initialValue);


        // maybe lose idless creation? default for now
        public static PositronicVariable<T> GetOrCreate(IPositronicRuntime runtime)
                => runtime.Factory.GetOrCreate<T>("default");

        /// <summary>
        /// Returns true if all registered positronic variables have converged.
        /// </summary>
        public static bool AllConverged(IPositronicRuntime rt)
        {
            bool all = rt.Registry.All(v => v.Converged() > 0);
            if (all)
                (rt as DefaultPositronicRuntime)?.FireAllConverged();
            return all;
        }

        public static IEnumerable<IPositronicVariable> GetAllVariables(IPositronicRuntime rt) => rt.Registry;

        /// <summary>
        /// Runs your code in a convergence loop until all variables have settled.
        /// </summary>
        public static void RunConvergenceLoop(
            IPositronicRuntime rt,
            Action code,
            bool runFinalIteration = true,
            bool unifyOnConvergence = true,
            bool bailOnFirstReverseWhenIdle = false)
        {
            var runtime = rt;
            var entropy = new DefaultEntropyController(runtime);
            var opsHandler = new RegretScribe<T>();
            var redirect = new SubEthaOutputTransponder(runtime);

            var engine = PositronicAmbient.Services.GetService<IImprobabilityEngine<T>>()
                    ?? new ImprobabilityEngine<T>(
                           new DefaultEntropyController(runtime),
                           new RegretScribe<T>(),
                           new SubEthaOutputTransponder(runtime),
                           runtime,
                           new BureauOfTemporalRecords<T>());

            // Before entering the loop, quarantine any outside-loop forward writes:
            foreach (var v in GetAllVariables(runtime).OfType<PositronicVariable<T>>())
                v.ResetTimelineIfOutsideWrites();

            QuantumLedgerOfRegret.Clear();
            BeginEpoch();

            try            {
                // mark "we're inside the loop" so the fast-path is disabled
                _loopDepth++;
                engine.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
            }
            finally
            {
                _loopDepth--;
                // After the engine finishes (including the final iteration),
                // clear the op log and re-seed domains from the *current* slice.
                if (runtime.Converged)
                {
                    foreach (var v in GetAllVariables(runtime).OfType<PositronicVariable<T>>())
                        v.ResetDomainToCurrent();
                    QuantumLedgerOfRegret.Clear();
                }
            }

        }

        /// <summary>
        /// Keep only the bootstrap slice if we had outside-loop writes; reset domain/flags accordingly.
        /// </summary>
        internal void ResetTimelineIfOutsideWrites()
        {
            var hasOutsideSlices = _sliceEpochs.Any(e => e == OutsideEpoch);
            if (!(_hadOutsideWritesSinceLastLoop || hasOutsideSlices))
                return;
            // Reset whenever we've observed outside writes since the last loop,
            // even if they were merged into a single slice.
            if (_hadOutsideWritesSinceLastLoop || hasOutsideSlices)
            {
                if (timeline.Count > 1)
                    timeline.RemoveRange(1, timeline.Count - 1);
                TruncateToBootstrapOnly();

                // Rebuild domain strictly from the bootstrap (no leakage from outside writes)
                _domain.Clear();
                foreach (var x in timeline[0].ToCollapsedValues())
                    _domain.Add(x);

                // Clear bookkeeping flags for a fresh convergence pass
                bootstrapSuperseded = false;
                _ops.SawForwardWrite = false;
                _hadOutsideWritesSinceLastLoop = false;

            }
            else
            {
                // Ensure the flag is cleared if nothing changed outside
                _hadOutsideWritesSinceLastLoop = false;
            }
        }



        // Helpers used by reverse replay so epoch tags never drift out of sync
        internal void AppendFromReverse(QuBit<T> qb)
        {
            timeline.Add(qb);
            StampAppendCurrentEpoch();
            OnTimelineAppended?.Invoke();
        }
        internal void ReplaceLastFromReverse(QuBit<T> qb)
        {
            timeline[^1] = qb;
            StampReplaceCurrentEpoch();
            OnTimelineAppended?.Invoke();
        }
        internal void ReplaceForwardHistoryWith(QuBit<T> qb)
        {
            if (timeline.Count > 1) { timeline.RemoveRange(1, timeline.Count - 1); TruncateToBootstrapOnly(); }
            timeline.Add(qb); StampAppendCurrentEpoch(); OnTimelineAppended?.Invoke();
        }



        /// <summary>
        /// Audits the quantum history to see if all timelines are finally tired of arguing and want to go home.
        /// </summary>
        public int Converged()
        {
            // If the engine itself already flagged full convergence, we're done.
            if (_runtime.Converged)
                return 1;

            if (timeline.Count < 3)
                return 0;

            // If the last two slices match, that's a 1‐step convergence.
            if (SameStates(timeline[^1], timeline[^2]))
                return 1;

            // Otherwise look for any earlier slice that matches the last.
            for (int i = 2; i <= timeline.Count; i++)
            {
                if (SameStates(timeline[^1], timeline[timeline.Count - i]))
                    return i - 1;
            }

            // No convergence detected yet.
            return 0;
        }


        /// <summary>
        /// Unifies all timeline slices into a single collapsed state.
        /// </summary>
        public void UnifyAll()
        {
            // nothing to do if we still only have the bootstrap
            if (timeline.Count < 2)
                return;

            /* Merge *all* values that appeared after the bootstrap (slice 0)
               into a single union slice, but **keep** slice 0 untouched so
               callers can still see the original seed when they ask for it. */

            var mergedStates = timeline
                                .Skip(1)   // ignore bootstrap
                                .SelectMany(q => q.ToCollapsedValues())
                                .Distinct()
                                .ToArray();

            var unified = new QuBit<T>(mergedStates);
            unified.Any();

            // replace everything after the bootstrap with the unified slice
            timeline.RemoveRange(1, timeline.Count - 1);
            timeline.Add(unified);

            // refresh the domain to reflect the new union
            _domain.Clear();
            foreach (var x in mergedStates)
                _domain.Add(x);

            _runtime.Converged = true;
            OnCollapse?.Invoke();
            OnConverged?.Invoke();

        }



        /// <summary>
        /// Unifies the last 'count' timeline slices into one.
        /// </summary>
        public void Unify(int count)
        {
            if (count < 2 || timeline.Count < count) return;
            int start = timeline.Count - count;
            var merged = timeline
                .Skip(start)
                .SelectMany(qb => qb.ToCollapsedValues())
                .Distinct()
                .ToList();

            timeline.RemoveRange(start, count);
            var newQb = new QuBit<T>(merged);
            newQb.Any();
            timeline.Add(newQb);
            _runtime.Converged = true;
            OnCollapse?.Invoke();
            OnConverged?.Invoke();
        }

        /// <summary>
        /// Assimilates the quantum state of another PositronicVariable into this one.
        /// </summary>
        public void Assign(PositronicVariable<T> other)
        {
            var qb = other.GetCurrentQBit();
            qb.Any();
            ReplaceOrAppendOrUnify(qb, replace: true);
        }

        /// <summary>
        /// Forcefully injects a scalar value into the timeline.
        /// </summary>
        public void Assign(T scalarValue)
        {
            var qb = new QuBit<T>(new[] { scalarValue });
            qb.Any();

            ReplaceOrAppendOrUnify(qb, replace: !isValueType);
        }

        /// <summary>
        /// Disambiguation overload so calls like <c>v.Assign(v + 1)</c> bind deterministically.
        /// Delegates to <see cref="Assign(QuBit{T})"/>.
        /// </summary>
        public void Assign(QExpr expr) => Assign(expr.Q);

        /// <summary>
        /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
        /// </summary>
        private void ReplaceOrAppendOrUnify(QuBit<T> qb, bool replace)
        {
            var runtime = _runtime;

            /* -------------------------------------------------------------
            *  Inside a convergence‑loop, forward half‑cycles follow
            *  different rules:
            *      - scalar  writes  (replace == false) **append**
            *      - qubit / union   (replace == true)  **overwrite**
            *    …but never touch the bootstrap slice.
            * ------------------------------------------------------------- */
            if (runtime.Entropy > 0 && InConvergenceLoop)
            {
                if (timeline.Count == 1) // still on bootstrap
                {
                    // Never mutate slice 0 during the loop; always append the first write.
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    return;
                }


                else if (replace)                            // overwrite/merge
                {
                    var incoming = qb.ToCollapsedValues();

                    // single‑value qubits are *scalar* writes → append
                    if (incoming.Count() == 1)
                    {
                        _temporalRecords.SnapshotAppend(this, qb);
                    }
                    else                // real union → keep the old merge path
                    {
                        var merged = timeline[^1]
                                        .ToCollapsedValues()
                                        .Union(incoming)
                                        .Distinct()
                                        .ToArray();
                        var mergedQb = new QuBit<T>(merged); mergedQb.Any();
                        _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    }

                    _ops.SawForwardWrite = true;
                    return;
                }
                else                                         // scalar - append
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                }
                return;
            }

            // We're going backwards in time, which means we can only see what will have going to have happened.
            if (runtime.Entropy < 0 && InConvergenceLoop)
            {
                // Always drive reverse replay; if caller passed the same instance, clone it.
                if (ReferenceEquals(qb, timeline[^1]))
                {
                    qb = new QuBit<T>(qb.ToCollapsedValues().ToArray());
                    qb.Any();
                }

                var topOp = QuantumLedgerOfRegret.Peek();
                if (topOp is null || topOp is MerlinFroMarker)
                    return;

                var rebuilt = _reverseReplay.ReplayReverseCycle(qb, this);
                AppendFromReverse(rebuilt);
                return;
            }

            // Emergency override for when we're flailing outside the loop like a time-traveling otter
            if (runtime.Entropy > 0 && !InConvergenceLoop)
            {
                // First ever forward write OR first after Unify → APPEND
                if (timeline.Count == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    bootstrapSuperseded = true;
                    _hadOutsideWritesSinceLastLoop = true;
                    return;
                }
                if (!replace)
                {
                    var lastStates = timeline[^1].ToCollapsedValues();
                    var merged = lastStates
                           .Union(qb.ToCollapsedValues())
                           // Only pull the bootstrap into the union when the last slice
                           // still represents a single scalar   (guarantees _pure_ scalar sequence)
                           .Union(lastStates.Count() == 1 ? timeline[0].ToCollapsedValues() : Array.Empty<T>())
                           .Distinct()
                           .ToArray();
                    var mergedQb = new QuBit<T>(merged); mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    _hadOutsideWritesSinceLastLoop = true;
                }
                else
                {
                    // Otherwise we need to preserve causality - else Marty McFly's hand will disappear again.
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    _hadOutsideWritesSinceLastLoop = true;
                }

                return;
            }

            // If we've already converged, do nothing.
            if (_runtime.Converged)
                return;

            Remember(qb.ToCollapsedValues());

            // --- Reverse‐time pass (Entropy < 0) -----------------------------
            if (runtime.Entropy < 0)
            {
                var top = QuantumLedgerOfRegret.Peek();
                if (top is null || top is MerlinFroMarker)
                    return;
                // OK, this is a true reverse-replay pass
                var rebuilt = _reverseReplay.ReplayReverseCycle(qb, this);
                AppendFromReverse(rebuilt);
                OnTimelineAppended?.Invoke();
                return;
            }

            // --- Forward‐time pass (Entropy > 0) -----------------------------
            if (runtime.Entropy > 0)
            {
                // — overwrite bootstrap if that's all we have
                if (bootstrapSuperseded && timeline.Count == 1)
                {
                    // ③ post‑Unify forward write should append, not overwrite
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    return;
                }

                // — first real forward write: snapshot+append
                if (!bootstrapSuperseded && timeline.Count == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    bootstrapSuperseded = true;
                    return;
                }

                // — overwrite (merge) if this is the very first forward write
                if (replace && !_ops.SawForwardWrite)
                {
                    var merged = timeline[^1]
                       .ToCollapsedValues()
                       .Union(qb.ToCollapsedValues())
                       .Distinct()
                       .ToArray();
                    var mergedQb = new QuBit<T>(merged);
                    mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    _ops.SawForwardWrite = true;
                    return;
                }
                // — otherwise decide append vs. merge
                if (!replace || (!_ops.SawForwardWrite && !SameStates(qb, timeline[^1])))
                {
                    // snapshot-append the new slice
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                }
                else
                {
                    // merge into the last slice if it's a duplicate scalar write
                    var merged = timeline[^1]
                                          .ToCollapsedValues()
                                          .Union(qb.ToCollapsedValues())
                                          .Distinct()
                                          .ToArray();
                    var mergedQb = new QuBit<T>(merged);
                    mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                }


                // — detect any small cycle and unify
                for (int cycle = 2; cycle <= 20; cycle++)
                    if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
                    {
                        Unify(cycle);
                        break;
                    }

                return;
            }

        }



        #region region Operator Overloads
        // --- Operator Overloads ---
        // --- Addition Overloads ---
        public static QExpr operator +(PositronicVariable<T> left, T right)
        {
            var resultQB = left.GetCurrentQBit() + right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator +(T left, PositronicVariable<T> right)
        {
            var resultQB = right.GetCurrentQBit() + left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator +(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                // Record addition using the collapsed value from the right variable.
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Modulus Overloads ---
        public static QuBit<T> operator %(PositronicVariable<T> left, T right)
        {
            var before = left.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = left.GetCurrentQBit() % right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
            return resultQB;
        }


        public static QuBit<T> operator %(T left, PositronicVariable<T> right)
        {
            var before = right.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = right.GetCurrentQBit() % left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
            return resultQB;
        }


        public static QuBit<T> operator %(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var before = left.GetCurrentQBit().ToCollapsedValues().First();
            var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
            return resultQB;
        }

        // --- Subtraction Overloads ---
        public static QuBit<T> operator -(PositronicVariable<T> left, T right)
        {
            var resultQB = left.GetCurrentQBit() - right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, right, left._runtime));
            }
            return resultQB;
        }

        public static QuBit<T> operator -(T left, PositronicVariable<T> right)
        {
            // Here, result = left - rightValue.
            var resultQB = right.GetCurrentQBit() - left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                // Use a reversed subtraction so that the inverse is: value = left - result.
                QuantumLedgerOfRegret.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
            }
            return resultQB;
        }

        public static QuBit<T> operator -(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, operand, left._runtime));
            }
            return resultQB;
        }

        // --- Unary Negation ---
        public static QuBit<T> operator -(PositronicVariable<T> value)
        {
            var qb = value.GetCurrentQBit();
            var negatedValues = qb.ToCollapsedValues().Select(v => (T)(-(dynamic)v)).ToArray();
            var negatedQb = new QuBit<T>(negatedValues);
            negatedQb.Any();
            if (value._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new NegationOperation<T>(value, value._runtime));
            }
            return negatedQb;
        }

        // --- Multiplication Overloads ---
        public static QuBit<T> operator *(PositronicVariable<T> left, T right)
        {
            var resultQB = left.GetCurrentQBit() * right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left, right, left._runtime));
            }
            return resultQB;
        }

        public static QuBit<T> operator *(T left, PositronicVariable<T> right)
        {
            var resultQB = right.GetCurrentQBit() * left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(right, left, right._runtime));
            }
            return resultQB;
        }

        public static QuBit<T> operator *(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() * right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left, operand, left._runtime));
            }
            return resultQB;
        }

        // --- Division Overloads ---
        public static QuBit<T> operator /(PositronicVariable<T> left, T right)
        {
            var currentQB = left.GetCurrentQBit();
            var resultQB = currentQB / right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, right, left._runtime));
            }
            return resultQB;
        }

        public static QuBit<T> operator /(T left, PositronicVariable<T> right)
        {
            var resultQB = right.GetCurrentQBit() / left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
            }
            return resultQB;
        }

        // --- Comparison Operators Using Comparer<T>.Default ---

        public static bool operator <(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) < 0;
        }
        public static bool operator >(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) > 0;
        }
        public static bool operator <=(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) <= 0;
        }
        public static bool operator >=(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) >= 0;
        }
        public static bool operator <(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) < 0;
        }
        public static bool operator >(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) > 0;
        }
        public static bool operator <=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) <= 0;
        }
        public static bool operator >=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) >= 0;
        }

        public static bool operator ==(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.SameStates(left.GetCurrentQBit(), right.GetCurrentQBit());
        }
        public static bool operator !=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return !(left == right);
        }
        public static bool operator ==(PositronicVariable<T> left, T right)
        {
            return left.GetCurrentQBit().ToCollapsedValues().First().Equals(right);
        }
        public static bool operator !=(PositronicVariable<T> left, T right)
        {
            return !(left == right);
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj is PositronicVariable<T> other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return GetCurrentQBit().ToCollapsedValues().Aggregate(0, (acc, x) => acc ^ (x?.GetHashCode() ?? 0));
        }

        /// <summary>
        /// Applies a binary function across two positronic variables, combining every possible future.
        /// </summary>
        public static PositronicVariable<T> Apply(Func<T, T, T> op, PositronicVariable<T> left, PositronicVariable<T> right, IPositronicRuntime runtime)
        {
            var leftValues = left.ToValues();
            var rightValues = right.ToValues();
            var results = leftValues.SelectMany(l => rightValues, (l, r) => op(l, r)).Distinct().ToArray();
            var newQB = new QuBit<T>(results);
            newQB.Any();
            return new PositronicVariable<T>(newQB, runtime);
        }

        /// <summary>
        /// Retrieves a specific slice from the timeline.
        /// </summary>
        public QuBit<T> GetSlice(int stepsBack)
        {
            if (stepsBack < 0 || stepsBack >= timeline.Count)
                throw new ArgumentOutOfRangeException(nameof(stepsBack));
            return timeline[timeline.Count - 1 - stepsBack];
        }

        /// <summary>
        /// Returns all timeline slices in order.
        /// </summary>
        public IEnumerable<QuBit<T>> GetTimeline() => timeline;

        /// <summary>
        /// Folds the last known state into a reality burrito and pretends none of the branching ever happened.
        /// </summary>
        public void CollapseToLastSlice()
        {
            var last = timeline.Last();
            var baseline = last.ToCollapsedValues().First();
            var collapsedQB = new QuBit<T>(new[] { baseline });
            collapsedQB.Any();
            timeline.Clear();
            timeline.Add(collapsedQB);
            OnCollapse?.Invoke();
        }

        /// <summary>
        /// Collapses the timeline using a custom strategy.
        /// </summary>
        public void CollapseToLastSlice(Func<IEnumerable<T>, T> strategy)
        {
            var last = timeline.Last();
            var chosenValue = strategy(last.ToCollapsedValues());
            var collapsedQB = new QuBit<T>(new[] { chosenValue });
            collapsedQB.Any();
            timeline.Clear();
            timeline.Add(collapsedQB);
            OnCollapse?.Invoke();
        }

        // --- Built-in collapse strategies ---
        public static Func<IEnumerable<T>, T> CollapseMin = values => values.Min();
        public static Func<IEnumerable<T>, T> CollapseMax = values => values.Max();
        public static Func<IEnumerable<T>, T> CollapseFirst = values => values.First();
        public static Func<IEnumerable<T>, T> CollapseRandom = values =>
        {
            var list = values.ToList();
            var rnd = new Random();
            return list[rnd.Next(list.Count)];
        };

        /// <summary>
        /// Creates a new branch by forking the timeline.
        /// </summary>
        public PositronicVariable<T> Fork(IPositronicRuntime runtime)
        {
            var forkedTimeline = timeline.Select(qb =>
            {
                var newQB = new QuBit<T>(qb.ToCollapsedValues().ToArray());
                newQB.Any();
                return newQB;
            }).ToList();

            var forked = new PositronicVariable<T>(forkedTimeline[0], runtime);
            forked.timeline.Clear();
            forkedTimeline.ForEach(qb => forked.timeline.Add(qb));
            return forked;
        }

        /// <summary>
        /// Forks the timeline and applies a transformation to the final slice.
        /// </summary>
        public PositronicVariable<T> Fork(Func<T, T> transform, IPositronicRuntime runtime)
        {
            var forked = Fork(runtime);
            var last = forked.timeline.Last();
            var transformedValues = last.ToCollapsedValues().Select(transform).ToArray();
            forked.timeline[forked.timeline.Count - 1] = new QuBit<T>(transformedValues);
            forked.timeline[forked.timeline.Count - 1].Any();
            return forked;
        }

        /// <summary>
        /// Outputs the history of this variable's midlife crises in a human-readable format.
        /// </summary>
        public string ToTimelineString()
        {
            return string.Join(Environment.NewLine,
                timeline.Select((qb, index) => $"Slice {index}: {qb}"));
        }

        /// <summary>
        /// Serializes the timeline to JSON.
        /// </summary>
        public string ExportToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(timeline, options);
        }

        /// <summary>
        /// A convenient wrapper to expose the current quantum values.
        /// </summary>
        public PositronicValueWrapper Value => new PositronicValueWrapper(GetCurrentQBit());

        public class PositronicValueWrapper
        {
            private readonly QuBit<T> qb;
            public PositronicValueWrapper(QuBit<T> q) => qb = q;
            public IEnumerable<T> ToValues() => qb.ToCollapsedValues();
        }

        /// <summary>
        /// Gets or sets the current quantum state.
        /// </summary>
        public QuBit<T> State
        {
            get => GetCurrentQBit();
            set => Assign(value);
        }

        /// <summary>
        /// Gets or sets the current scalar value, collapsing if necessary.
        /// </summary>
        public T Scalar
        {
            get => GetCurrentQBit().ToCollapsedValues().First();
            set => Assign(value);
        }


        /// <summary>
        /// Collapses the current quantum cloud into discrete values.
        /// </summary>
        public IEnumerable<T> ToValues() => GetCurrentQBit().ToCollapsedValues();

        /// <summary>
        /// Retrieves the freshest slice of existence, still warm from the cosmic oven.
        /// Return the "current" qubit:
        ///  - outside the loop: last slice wins (legacy behavior)
        ///  - inside the loop : last slice of the *current epoch*
        /// </summary>
        public QuBit<T> GetCurrentQBit()
        {
            if (timeline.Count == 0)
                throw new InvalidOperationException("Timeline is empty.");

            if (!InConvergenceLoop)
                return timeline[^1];                  // legacy outside-loop semantics

            // Inside the loop: prefer the freshest slice from the current epoch.
            var epoch = CurrentEpoch;
            // Defensive: keep index in bounds even if something goes off the rails.
            int lastIdx = Math.Min(timeline.Count, _sliceEpochs.Count) - 1;
            for (int i = lastIdx; i >= 0; i--)
            {
                if (_sliceEpochs[i] == epoch)
                    return timeline[i];
            }

            // Fallback: if no slice is tagged (shouldn't happen because the engine pre-appends a reverse slice),
            // use the most recent slice rather than exploding.
            return timeline[lastIdx];
        }


        /// <summary>
        /// Checks whether two QuBits represent the same state.
        /// </summary>
        private bool SameStates(QuBit<T> a, QuBit<T> b)
        {
            var av = a.ToCollapsedValues().OrderBy(x => x).ToList();
            var bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
            if (av.Count != bv.Count) return false;
            for (int i = 0; i < av.Count; i++)
                if (!Equals(av[i], bv[i]))
                    return false;
            return true;
        }

        public override string ToString()
        {
            return GetCurrentQBit().ToString();
        }

        /// <summary>
        /// (Called by the engine after convergence)
        /// Wipe out the internal _domain and re-seed it from the current QBit alone.
        /// </summary>
        internal void ResetDomainToCurrent()
        {
            _domain.Clear();
            foreach (var x in GetCurrentQBit().ToCollapsedValues())
                _domain.Add(x);
        }

        /// <summary>
        /// Marks this variable as having had outside-loop forward writes.
        /// On the next RunConvergenceLoop(), ResetTimelineIfOutsideWrites() will
        /// quarantine to the bootstrap slice. This does NOT mutate the timeline.
        /// </summary>
        public void NoteOutsideWrites()
        {
            _hadOutsideWritesSinceLastLoop = true;
        }



        /// <summary>
        /// A helper struct to enable chained operations without immediate collapse.
        /// </summary>
        public readonly struct QExpr
        {
            internal readonly QuBit<T> Q;
            internal readonly PositronicVariable<T> Source;
            internal QExpr(PositronicVariable<T> src, QuBit<T> q) { Source = src; Q = q; }
            public IEnumerable<T> ToCollapsedValues() => Q.ToCollapsedValues();
            public override string ToString() => Q.ToString();
            public static implicit operator QuBit<T>(QExpr e) => e.Q;

            /// <summary>
            /// (PositronicVariable<T> = QExpr) - e.g., antival = (antival + 1)
            /// </summary>
            /// <param name="e"></param>
            public static implicit operator PositronicVariable<T>(QExpr e)
            {
                e.Source.Assign(e.Q);
                return e.Source;
            }

            /// <summary>
            /// (QExpr % T) - e.g., (antival + 1) % 3
            /// </summary>
            public static QuBit<T> operator %(QExpr left, T right)
            {
                var resultQB = left.Q % right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, right, left.Source._runtime));
                return resultQB;
            }


            /// <summary>
            /// (QExpr % PositronicVariable<T>) - optional, for parity with PV%PV
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static QuBit<T> operator %(QExpr left, PositronicVariable<T> right)
            {
                var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
                var resultQB = left.Q % right.GetCurrentQBit();
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, divisor, left.Source._runtime));
                return resultQB;
            }


        }

    }
}
