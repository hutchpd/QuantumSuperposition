using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Engine;
using PositronicVariables.Engine.Entropy;
using PositronicVariables.Engine.Logging;
using PositronicVariables.Engine.Timeline;
using PositronicVariables.Engine.Transponder;
using PositronicVariables.Initialisation;
using PositronicVariables.Operations;
using PositronicVariables.Runtime;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        private static int OutsideEpoch => -1;

        private static int s_LastEntropySeenForEpoch = int.MaxValue; // forces a bump on first write

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
        private bool _sawStateReadThisForward = false;
        private static readonly bool s_IsIntegralType =
            typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
            typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
            typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
            typeof(T) == typeof(long) || typeof(T) == typeof(ulong);
        private static readonly bool s_IsFloatingType =
            typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal);

        private static bool IsZero(dynamic v) { try { return v == 0; } catch { return false; } }
        private static bool IsMinus1(dynamic v) { try { return v == -1; } catch { return false; } }

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
            _temporalRecords.OverwriteBootstrap(this, qb);
            _sliceEpochs.Clear();      // epoch tagging for slice[0]
            _sliceEpochs.Add(0);       // bootstrap marked as epoch 0

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
            _sliceEpochs.Add(0);

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
        /// Injects a quantum state into the timeline and pretends like that was always the plan.
        /// </summary>
        public void Assign(QuBit<T> qb)
        {
            if (qb is null)
                throw new ArgumentNullException(nameof(qb));

            qb.Any();

            ReplaceOrAppendOrUnify(qb, replace: true);
            _sawStateReadThisForward = false;
        }

        /// <summary>
        /// Forcefully injects a single scalar value into the timeline.
        /// </summary>
        public void Assign(T scalarValue)
        {
            Assign(new[] { scalarValue });
        }

        /// <summary>
        /// Forcefully injects scalar value(s) into the timeline.
        /// </summary>
        public void Assign(params T[] scalarValues)
        {
            if (scalarValues == null || scalarValues.Length == 0)
                throw new ArgumentException("At least one value must be provided.");

            var qb = new QuBit<T>(scalarValues);
            qb.Any();
            ReplaceOrAppendOrUnify(qb, replace: !isValueType);
        }

        // Automatically enable simulation on first use.
        private static readonly bool _ = EnableSimulation();
        private static bool EnableSimulation()
        {
            NeuroCascadeInitialiser.AutoEnable();
            return true;
        }



        // The timeline of quantum slices.
        public readonly List<QuBit<T>> timeline = new List<QuBit<T>>();
        private bool bootstrapSuperseded = false;

        // Epoch helpers
        internal static void BeginEpoch() => s_CurrentEpoch++;
        internal static int CurrentEpoch => s_CurrentEpoch;

        internal void StampBootstrap() { _sliceEpochs.Clear(); _sliceEpochs.Add(0); }
        internal void StampAppendCurrentEpoch()
            => _sliceEpochs.Add(InConvergenceLoop ? CurrentEpoch : OutsideEpoch);
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


            // Before entering the loop, quarantine any forward writes that happened outside the loop
            // during the "first run" (console style).
            foreach (var v in GetAllVariables(runtime).OfType<PositronicVariable<T>>())
                v.ResetTimelineIfOutsideWrites();


            QuantumLedgerOfRegret.Clear();
            BeginEpoch();
            s_LastEntropySeenForEpoch = int.MaxValue; // forces a bump on first write

            try
            {
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
        /// If we observed outside-loop writes, drop everything after the bootstrap
        /// so the convergence pass starts from a clean slate.
        /// </summary>
        internal void ResetTimelineIfOutsideWrites()
        {
            bool sawOutsideEpochs = _sliceEpochs.Any(e => e == OutsideEpoch);
            if (!(_hadOutsideWritesSinceLastLoop || sawOutsideEpochs))
                return;

            if (timeline.Count > 1)
                timeline.RemoveRange(1, timeline.Count - 1);
            TruncateToBootstrapOnly();

            // Rebuild domain strictly from the bootstrap (no leakage from outside writes)
            _domain.Clear();
            foreach (var x in timeline[0].ToCollapsedValues())
                _domain.Add(x);

            // Clear bookkeeping for the fresh pass
            bootstrapSuperseded = false;
            _ops.SawForwardWrite = false;
            _hadOutsideWritesSinceLastLoop = false;
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

            // keep epochs in sync: drop stamps past bootstrap
            if (_sliceEpochs.Count > 1)
                _sliceEpochs.RemoveRange(1, _sliceEpochs.Count - 1);

            timeline.Add(unified);
            StampAppendCurrentEpoch();  // tag the unified slice for the current epoch (or OutsideEpoch if not in-loop)

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

            // keep epochs in sync with the removal
            if (_sliceEpochs.Count > start)
                _sliceEpochs.RemoveRange(start, _sliceEpochs.Count - start);

            var newQb = new QuBit<T>(merged);
            newQb.Any();
            timeline.Add(newQb);
            StampAppendCurrentEpoch();

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
        /// Disambiguation overload so calls like <c>v.Assign(v + 1)</c> bind deterministically.
        /// Delegates to <see cref="Assign(QuBit{T})"/>.
        /// </summary>
        public void Assign(QExpr expr)
        {
            var isFeedbackFromSelf = ReferenceEquals(this, expr.Source);
            var qb = (QuBit<T>)expr; // force materialization (lazy or eager)
            ReplaceOrAppendOrUnify(qb, replace: true, isFeedbackFromSelf);
            _sawStateReadThisForward = false;
        }

        // Unify small cycles (2..20) when the last slice repeats a prior slice.
        private void DetectAndUnifySmallCycle()
        {
            for (int cycle = 2; cycle <= 20; cycle++)
            {
                if (timeline.Count >= cycle + 1 &&
                    SameStates(timeline[^1], timeline[^(cycle + 1)]))
                {
                    Unify(cycle);
                    break;
                }
            }
        }


        /// <summary>
        /// Either replaces, appends, or unifies the current timeline slice with a new quantum slice.
        /// </summary>
        private void ReplaceOrAppendOrUnify(QuBit<T> qb, bool replace, bool isFeedbackFromSelf = false)
        {
            if (InConvergenceLoop)
            {
                var e = _runtime.Entropy;
                if (e != s_LastEntropySeenForEpoch)
                {
                    BeginEpoch();                       // new epoch for this half-cycle
                    s_LastEntropySeenForEpoch = e;
                    ResetReverseReplayFlag();
                }
            }


            var runtime = _runtime;

            /* -------------------------------------------------------------
            *  Inside a convergence‑loop, forward half‑cycles follow
            *  different rules:
            *      - scalar  writes  (replace == false) **append**
            *      - qubit / union   (replace == true)  **overwrite**
            *    …but never touch the bootstrap slice.
            * ------------------------------------------------------------- */
            // --- Forward, in-loop -------------------------------------------------
            if (runtime.Entropy > 0 && InConvergenceLoop)
            {
                if (replace && isFeedbackFromSelf && timeline.Count > 0
                    && !_ops.SawForwardWrite                // first write this forward half-cycle
                    && !_sawStateReadThisForward)           // not a `.State = ...` mutation
                {
                    var incoming = qb.ToCollapsedValues().ToArray();
                    var last = timeline[^1].ToCollapsedValues().ToArray();

                    bool bothScalar = incoming.Length == 1 && last.Length == 1;
                    if (bothScalar)
                    {
                        var unionValues = last.Union(incoming).Distinct().ToArray();
                        var unionQb = new QuBit<T>(unionValues); unionQb.Any();

                        if (timeline.Count == 1)
                        {
                            _temporalRecords.SnapshotAppend(this, unionQb);
                            StampAppendCurrentEpoch();
                        }
                        else
                        {
                            _temporalRecords.ReplaceLastSlice(this, unionQb);
                            StampReplaceCurrentEpoch();
                        }

                        _ops.SawForwardWrite = true;
                        DetectAndUnifySmallCycle();
                        return;
                    }
                    // else: fall through to normal handling
                }
                // Self-feedback early-union for the first forward write (negation case etc.)
                if (replace && isFeedbackFromSelf && timeline.Count > 0 && _ops.SawForwardWrite)
                {
                    var incoming = qb.ToCollapsedValues().ToArray();
                    bool incomingIsScalar = incoming.Length == 1;

                    // If the current slice is already a set (non-scalar), we can safely
                    // union with the incoming feedback to keep growing the set.
                    bool lastIsNonScalar = timeline[^1].ToCollapsedValues().Skip(1).Any();

                    if (lastIsNonScalar && !incomingIsScalar)
                    {
                        // Both sides are sets → union them.
                        var unionValues = timeline[^1].ToCollapsedValues()
                                                     .Union(incoming)
                                                     .Distinct()
                                                     .ToArray();
                        var unionQb = new QuBit<T>(unionValues); unionQb.Any();

                        _temporalRecords.ReplaceLastSlice(this, unionQb);
                        StampReplaceCurrentEpoch();

                        _ops.SawForwardWrite = true;
                        DetectAndUnifySmallCycle();
                        return;
                    }

                    // If last is a set but incoming is scalar, also merge (we're still exploring the set).
                    if (lastIsNonScalar && incomingIsScalar)
                    {
                        var unionValues = timeline[^1].ToCollapsedValues()
                                                     .Union(incoming)
                                                     .Distinct()
                                                     .ToArray();
                        var unionQb = new QuBit<T>(unionValues); unionQb.Any();

                        _temporalRecords.ReplaceLastSlice(this, unionQb);
                        StampReplaceCurrentEpoch();

                        _ops.SawForwardWrite = true;
                        DetectAndUnifySmallCycle();
                        return;
                    }

                    // Otherwise: first write this half-cycle or last is scalar → fall through
                    // and let normal scalar/append/replace rules apply.
                }

                if (timeline.Count == 1)
                {
                    // Never mutate the bootstrap during the loop; append the first write.
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
                    _ops.SawForwardWrite = true;
                    DetectAndUnifySmallCycle();
                    return;
                }

                if (replace)
                {
                    var incoming = qb.ToCollapsedValues().ToArray();
                    bool secondPlusScalar = incoming.Length == 1 && _ops.SawForwardWrite;
                    bool shouldAppend = !secondPlusScalar || isFeedbackFromSelf;

                    if (shouldAppend)
                    {
                        _temporalRecords.SnapshotAppend(this, qb);
                        StampAppendCurrentEpoch();
                    }
                    else
                    {
                        _temporalRecords.ReplaceLastSlice(this, qb);
                        StampReplaceCurrentEpoch();
                    }

                    _ops.SawForwardWrite = true;
                    DetectAndUnifySmallCycle();
                    return;
                }

                // scalar (replace == false) → append
                _temporalRecords.SnapshotAppend(this, qb);
                StampAppendCurrentEpoch();
                _ops.SawForwardWrite = true;
                DetectAndUnifySmallCycle();
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

                if (isFeedbackFromSelf && timeline.Count > 0)
                {
                    var union = rebuilt.ToCollapsedValues()
                                       .Union(timeline[^1].ToCollapsedValues())
                                       .Distinct()
                                       .ToArray();
                    rebuilt = new QuBit<T>(union);
                    rebuilt.Any();
                }

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
                    StampAppendCurrentEpoch();
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
                    StampReplaceCurrentEpoch();
                    _hadOutsideWritesSinceLastLoop = true;
                }
                else
                {
                    // Otherwise we need to preserve causality - else Marty McFly's hand will disappear again.
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
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
                    StampReplaceCurrentEpoch();
                    _ops.SawForwardWrite = true;
                    return;
                }
                // — otherwise decide append vs. merge
                if (!replace || (!_ops.SawForwardWrite && !SameStates(qb, timeline[^1])))
                {
                    // snapshot-append the new slice
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
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
                    StampReplaceCurrentEpoch();
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

        // --- Subtraction Overloads ---
        public static QExpr operator -(PositronicVariable<T> left, T right)
        {
            var resultQB = left.GetCurrentQBit() - right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator -(T left, PositronicVariable<T> right)
        {
            // Here, result = left - rightValue.
            var resultQB = right.GetCurrentQBit() - left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                // Use a reversed subtraction so that the inverse is: value = left - result.
                QuantumLedgerOfRegret.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator -(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Unary Negation ---
        public static QExpr operator -(PositronicVariable<T> value)
        {
            var qb = value.GetCurrentQBit();

            var negatedValues = qb
                .ToCollapsedValues()
                .Select(v =>
                {
                    dynamic dv = v;
                    return (T)(-dv);
                })
                .ToArray();

            var negatedQb = new QuBit<T>(negatedValues);
            negatedQb.Any();
            if (value._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new NegationOperation<T>(value, value._runtime));
            }
            return new QExpr(value, negatedQb);
        }

        // --- Multiplication Overloads ---
        public static QExpr operator *(PositronicVariable<T> left, T right)
        {
            // Build from the freshest slice when the expression is *used*
            Func<QuBit<T>> lazy = () =>
            {
                var srcQB = left.GetCurrentQBit();
                srcQB.Any();
                var resultQB = srcQB * right;
                resultQB.Any();
                return resultQB;
            };

            if (left._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(
                IsMinus1(right)
                               ? new NegationOperation<T>(left, left._runtime)
                               : new MultiplicationOperation<T>(left, right, left._runtime));

            return new QExpr(left, lazy);
        }

        public static QExpr operator *(T left, PositronicVariable<T> right)
        {
            // Build from the freshest slice when the expression is *used*
            Func<QuBit<T>> lazy = () =>
            {
                var srcQB = right.GetCurrentQBit();
                srcQB.Any();
                var multiplied = srcQB
                    .ToCollapsedValues()
                    .Select(v =>
                    {
                        dynamic dl = left;
                        dynamic dv = v;
                        return (T)(dl * dv);
                    })
                    .Distinct()
                    .ToArray();

                var resultQB = new QuBit<T>(multiplied);
                resultQB.Any();
                return resultQB;
            };

            if (right._runtime.Entropy >= 0)
               QuantumLedgerOfRegret.Record(
                   IsMinus1(left)
                       ? new NegationOperation<T>(right, right._runtime)
                       : new MultiplicationOperation<T>(right, left, right._runtime));

            return new QExpr(right, lazy);
        }

        public static QExpr operator *(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() * right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                if (IsMinus1(operand))
                    QuantumLedgerOfRegret.Record(new NegationOperation<T>(left, left._runtime));
                else
                    QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Division Overloads ---
        public static QExpr operator /(PositronicVariable<T> left, T right)
        {
            var currentQB = left.GetCurrentQBit();
            var resultQB = currentQB / right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator /(T left, PositronicVariable<T> right)
        {
            var resultQB = right.GetCurrentQBit() / left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator /(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var resultQB = left.GetCurrentQBit() / right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Modulus Overloads ---
        public static QExpr operator %(PositronicVariable<T> left, T right)
        {
            var before = left.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = left.GetCurrentQBit() % right;
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
            return new QExpr(left, resultQB);
        }

        public static QExpr operator %(T left, PositronicVariable<T> right)
        {
            var before = right.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = right.GetCurrentQBit() % left;
            resultQB.Any();
            if (right._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
            return new QExpr(right, resultQB);
        }

        public static QExpr operator %(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            var before = left.GetCurrentQBit().ToCollapsedValues().First();
            var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
            var resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
            resultQB.Any();
            if (left._runtime.Entropy >= 0)
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
            return new QExpr(left, resultQB);
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
        /// Returns a QExpr to ensure arithmetic operations are logged correctly.
        /// </summary>
        public QExpr State
        {
            get
            {
                if (InConvergenceLoop && _runtime.Entropy > 0) _sawStateReadThisForward = true;
                return new QExpr(this, GetCurrentQBit());
            }
            set => Assign(value);  // QExpr has implicit conversion through Assign(QExpr)
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
            int lastStamped = Math.Min(timeline.Count, _sliceEpochs.Count) - 1;
            for (int i = lastStamped; i >= 0; i--)
            {
                if (_sliceEpochs[i] == epoch)
                    return timeline[i];
            }
            // Fallback when tags are missing or out of sync (e.g., after Unify):
            return timeline[^1];  // newest slice wins

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
            internal readonly PositronicVariable<T> Source;

            // Eager result (old behavior)
            internal readonly QuBit<T> Q;

            // Lazy materializer (new behavior) – when present, we build the QuBit at use-time
            private readonly Func<QuBit<T>> _lazy;
            private readonly bool _isLazy;

            internal QExpr(PositronicVariable<T> src, QuBit<T> q)
            {
                Source = src;
                Q = q;
                _lazy = null;
                _isLazy = false;
            }

            // New ctor: build on demand from the Source's *current* qubit
            internal QExpr(PositronicVariable<T> src, Func<QuBit<T>> lazy)
            {
                Source = src;
                Q = default!;
                _lazy = lazy;
                _isLazy = true;
            }

            private QuBit<T> Resolve()
            {
                var qb = _isLazy ? _lazy() : Q;
                qb.Any(); // ensure union-aware enumeration/printing
                return qb;
            }

            public IEnumerable<T> ToCollapsedValues() => Resolve().ToCollapsedValues();
            public override string ToString() => Resolve().ToString();

            public static implicit operator QuBit<T>(QExpr e) => e.Resolve();

            public static implicit operator PositronicVariable<T>(QExpr e)
            {
                e.Source.Assign(e.Resolve());
                return e.Source;
            }

            // ---------- QExpr ⊗ scalar operators ----------
            public static QExpr operator %(QExpr left, T right)
            {
                var resultQB = left.Resolve() % right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, right, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }

            public static QExpr operator +(QExpr left, T right)
            {
                var resultQB = left.Resolve() + right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left.Source, right, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }

            public static QExpr operator -(QExpr left, T right)
            {
                var resultQB = left.Resolve() - right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left.Source, right, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }

            public static QExpr operator *(QExpr left, T right)
            {
                var resultQB = left.Resolve() * right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left.Source, right, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }

            public static QExpr operator /(QExpr left, T right)
            {
                var resultQB = left.Resolve() / right;
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left.Source, right, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }

            // ---------- QExpr % PositronicVariable<T> ----------
            public static QExpr operator %(QExpr left, PositronicVariable<T> right)
            {
                var divisor = right.GetCurrentQBit().ToCollapsedValues().First();
                var resultQB = left.Resolve() % right.GetCurrentQBit();
                resultQB.Any();
                if (left.Source._runtime.Entropy >= 0)
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, divisor, left.Source._runtime));
                return new QExpr(left.Source, resultQB);
            }
        }

    }
}
