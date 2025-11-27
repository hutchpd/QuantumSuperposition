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
using System.Linq;
using System.Text.Json;
using PositronicVariables.Transactions;
using System.Threading;

namespace PositronicVariables.Variables
{
    /// <summary>
    /// A "positronic" variable that remembers everything it's ever been, and a few things it hasn't.
    /// If Schrödinger and Asimov had a baby, this would be its teething ring.
    /// </summary>
    /// <typeparam name="T">A type that implements IComparable.</typeparam>
    public partial class PositronicVariable<T> : IPositronicVariable, ITransactionalVariable
        where T : IComparable<T>
    {
        [ThreadStatic]
        private static bool s_SuppressOperatorLogging;
        // --- TVar fields (Stage 2) ---
        private static long s_GlobalTVarId = 0;
        private readonly object _tvarLock = new object();
        private readonly long _tvarId;
        private long _tvarVersion;

        long ITransactionalVariable.TxId => _tvarId;
        long ITransactionalVariable.TxVersion => Volatile.Read(ref _tvarVersion);
        object ITransactionalVariable.TxLock => _tvarLock;
        void ITransactionalVariable.TxApplyRequired(object qb) => ConstrainEqual((QuBit<T>)qb);
        void ITransactionalVariable.TxBumpVersion() => Interlocked.Increment(ref _tvarVersion);

        private static int OutsideEpoch => -1;

        private static int s_LastEntropySeenForEpoch = int.MaxValue; // kick the epoch counter awake on first write
        private bool _hasForwardScalarBaseline = false;
        private T _forwardScalarBaseline;

        private bool _hadRequiredThisForward = false;
        private readonly bool isValueType = typeof(T).IsValueType;
        private readonly HashSet<T> _domain = [];
        private static bool _reverseReplayStarted;
        private static int s_LoopDepth;
        private readonly IPositronicRuntime _runtime;
        private readonly List<int> _sliceEpochs = [];
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

        internal void NotifyFirstAppend()
        {
            bootstrapSuperseded = true;
        }

        internal static bool InConvergenceLoop => System.Threading.Volatile.Read(ref s_LoopDepth) > 0;
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
                        // Spin up a pocket universe (minimal host) and wire in the default runtime.
                        IHostBuilder hb = Host.CreateDefaultBuilder()
                                     .ConfigureServices(s => s.AddPositronicRuntime());
                        PositronicAmbient.InitialiseWith(hb);
                    }
                }
            }
            return PositronicAmbient.Current;
        }

        public static PositronicVariable<T> GetOrCreate(string id, T initialValue)
        {
            return GetOrCreate(id, initialValue, EnsureAmbientRuntime());
        }

        public static PositronicVariable<T> GetOrCreate(string id)
        {
            return GetOrCreate(id, EnsureAmbientRuntime());
        }

        public static PositronicVariable<T> GetOrCreate(T initialValue)
        {
            return GetOrCreate(initialValue, EnsureAmbientRuntime());
        }

        public static PositronicVariable<T> GetOrCreate()
        {
            return GetOrCreate(EnsureAmbientRuntime());
        }

        public void SeedBootstrap(params T[] values)
        {
            QuBit<T> qb = new(values);
            qb.Any();
            _temporalRecords.OverwriteBootstrap(this, qb);
            _sliceEpochs.Clear();      // stamp slice[0] as Epoch Zero (the Big Bang)
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

            _temporalRecords = timelineArchivist ?? new BureauOfTemporalRecords<T>();
            QuBit<T> qb = new([initialValue]);

            _temporalRecords.OverwriteBootstrap(this, qb);
            _sliceEpochs.Clear();
            _sliceEpochs.Add(0);

            _domain.Add(initialValue);
            _runtime.Registry.Add(this);

            // Hire our cosmic bureaucrats via DI (archivist + scribe).
            _ops = opsHandler ?? new RegretScribe<T>();
            _reverseReplay = new UnhappeningEngine<T>(_ops, _temporalRecords);

            // When the timeline grows a new limb, let the world know.
            _temporalRecords.RegisterTimelineAppendedHook(() => OnTimelineAppended?.Invoke());

            // TVar init
            _tvarId = Interlocked.Increment(ref s_GlobalTVarId);
            _tvarVersion = 0;
        }

        static PositronicVariable()
        {
            // Entrance exam: types must be comparable to navigate the multiverse responsibly.
            if (!(typeof(IComparable).IsAssignableFrom(typeof(T)) ||
                  typeof(IComparable<T>).IsAssignableFrom(typeof(T))))
            {
                throw new InvalidOperationException(
                    $"Type parameter '{typeof(T).Name}' for PositronicVariable " +
                    "must implement IComparable or IComparable<T> to enable proper convergence checking.");
            }

            // Wake the neural lattice so the chronometers start ticking.
            bool _ = AethericRedirectionGrid.Initialised;
        }

        private void Remember(IEnumerable<T> xs)
        {
            foreach (T x in xs)
            {
                _domain.Add(x);
            }
        }

        internal static void ResetReverseReplayFlag()
        {
            _reverseReplayStarted = false;           // Reset the "we're about to moonwalk through time" alarm.
        }

        /// <summary>
        /// Fires when a new timeline is stitched onto this unfortunate beast,
        /// like bolting a new memory onto Frankenstein's hippocampus.
        /// </summary>
        internal static Action TimelineAppendedHook;

        /// <summary>
        /// Injects a quantum state into the timeline and pretends like that was always the plan.
        /// </summary>
        public void Assign(QuBit<T> qb)
        {
            ArgumentNullException.ThrowIfNull(qb);

            // Non-transactional write: guard with per-variable lock and bump version after apply
            lock (_tvarLock)
            {
                ReplaceOrAppendOrUnify(qb, replace: true);
                _sawStateReadThisForward = false;
                Interlocked.Increment(ref _tvarVersion);
            }
        }

        /// <summary>
        /// Kicks a lone scalar through the event horizon and hopes the timeline doesn't notice.
        /// </summary>
        public void Assign(T scalarValue)
        {
            Assign([scalarValue]);
        }

        /// <summary>
        /// Kicks a set of scalar values through the event horizon and hopes the timeline doesn't notice.
        /// </summary>
        public void Assign(params T[] scalarValues)
        {
            if (scalarValues == null || scalarValues.Length == 0)
            {
                throw new ArgumentException("At least one value must be provided.");
            }

            QuBit<T> qb = new(scalarValues);

            lock (_tvarLock)
            {
                ReplaceOrAppendOrUnify(qb, replace: !isValueType);
                Interlocked.Increment(ref _tvarVersion);
            }
        }

        // Simulation is enabled by default, like a toddler with a vivid imagination.
        private static readonly bool _ = EnableSimulation();
        private static bool EnableSimulation()
        {
            NeuroCascadeInitialiser.AutoEnable();
            return true;
        }



        /// <summary>
        /// The timeline of quantum slices, each a breadcrumb trail through the multiverse's tangled yarn.
        /// </summary>
        public readonly List<QuBit<T>> timeline = [];
        private bool bootstrapSuperseded = false;

        // Epoch helpers
        internal static void BeginEpoch()
        {
            CurrentEpoch++;
        }

        internal static int CurrentEpoch { get; private set; } = 0;

        internal void StampBootstrap() { _sliceEpochs.Clear(); _sliceEpochs.Add(0); }
        internal void StampAppendCurrentEpoch()
        {
            _sliceEpochs.Add(InConvergenceLoop ? CurrentEpoch : OutsideEpoch);
        }

        internal void StampReplaceCurrentEpoch()
        {
            if (_sliceEpochs.Count == 0)
            {
                _sliceEpochs.Add(0);
            }

            _sliceEpochs[^1] = InConvergenceLoop ? CurrentEpoch : OutsideEpoch;
        }
        internal void TruncateToBootstrapOnly()
        {
            if (_sliceEpochs.Count > 1)
            {
                _sliceEpochs.RemoveRange(1, _sliceEpochs.Count - 1);
            }
        }

        public event Action OnConverged;
        public event Action OnCollapse;
        public event Action OnTimelineAppended;



        /// <summary>
        /// How many alternate realities we've stacked on this poor variable's timeline. More is usually bad.
        /// </summary>
        public int TimelineLength => timeline.Count;

        /// <summary>
        /// Politely asks the universe which way time is flowing today.
        /// It rarely answers. Just pretends it's forward.
        /// </summary>
        /// <param name="rt">The current runtime</param>
        /// <param name="e">Entropy direction -1 for backwards, 1 for forwards.</param>
        public static void SetEntropy(IPositronicRuntime rt, int e)
        {
            rt.Entropy = e;
        }

        /// <summary>
        /// Attempts to read the universal vibe setting. Forward? Backward? Existential spiral?
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public static int GetEntropy(IPositronicRuntime rt)
        {
            return rt.Entropy;
        }

        public static PositronicVariable<T> GetOrCreate(string id, T initialValue, IPositronicRuntime runtime)
        {
            return runtime.Factory.GetOrCreate<T>(id, initialValue);
        }

        public static PositronicVariable<T> GetOrCreate(string id, IPositronicRuntime runtime)
        {
            return runtime.Factory.GetOrCreate<T>(id);
        }

        public static PositronicVariable<T> GetOrCreate(T initialValue, IPositronicRuntime runtime)
        {
            return runtime.Factory.GetOrCreate<T>(initialValue);
        }

        public static PositronicVariable<T> GetOrCreate(IPositronicRuntime runtime)
        {
            return runtime.Factory.GetOrCreate<T>("default");
        }

        /// <summary>
        /// Returns true if all variables have achieved spiritual enlightenment or just got tired of diverging.
        /// </summary>
        public static bool AllConverged(IPositronicRuntime rt)
        {
            bool all = rt.Registry.All(v => v.Converged() > 0);
            if (all)
            {
                (rt as DefaultPositronicRuntime)?.FireAllConverged();
            }

            return all;
        }

        /// <summary>
        /// Fetches every poor soul currently pretending to be a variable in this runtime.
        /// </summary>
        public static IEnumerable<IPositronicVariable> GetAllVariables(IPositronicRuntime rt)
        {
            return rt.Registry;
        }

        /// <summary>
        /// Forces reality to stabilize, through brute repetition and caffeine.
        /// </summary>
        public static void RunConvergenceLoop(
            IPositronicRuntime rt,
            Action code,
            bool runFinalIteration = true,
            bool unifyOnConvergence = true,
            bool bailOnFirstReverseWhenIdle = false)
        {
            IPositronicRuntime runtime = rt;
            DefaultEntropyController entropy = new(runtime);
            RegretScribe<T> opsHandler = new();
            SubEthaOutputTransponder redirect = new(runtime);

            IImprobabilityEngine<T> engine = PositronicAmbient.Services.GetService<IImprobabilityEngine<T>>()
                    ?? new ImprobabilityEngine<T>(
                           new DefaultEntropyController(runtime),
                           new RegretScribe<T>(),
                           new SubEthaOutputTransponder(runtime),
                           runtime,
                           new BureauOfTemporalRecords<T>());


            // Before the loop: quarantine any forward writes that happened in the "pilot episode" (outside-loop run).
            foreach (PositronicVariable<T> v in GetAllVariables(runtime).OfType<PositronicVariable<T>>())
            {
                v.ResetTimelineIfOutsideWrites();
            }

            QuantumLedgerOfRegret.Clear();
            BeginEpoch();
            s_LastEntropySeenForEpoch = int.MaxValue; // ensure the first write bumps the epoch

            try
            {
                // Mark that we're inside the time vortex so fast-path antics are disabled.
                s_LoopDepth++;
                engine.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
            }
            finally
            {
                s_LoopDepth--;
                // When the engine lands (including any final lap), clear the logbook
                // and rebuild each domain from whatever timeline slice we're currently calling "reality".
                if (runtime.Converged)
                {
                    foreach (PositronicVariable<T> v in GetAllVariables(runtime).OfType<PositronicVariable<T>>())
                    {
                        v.ResetDomainToCurrent();
                    }

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
            {
                return;
            }

            if (timeline.Count > 1)
            {
                timeline.RemoveRange(1, timeline.Count - 1);
            }

            TruncateToBootstrapOnly();

            // Rebuild domain strictly from the bootstrap. No leakage from the wild frontier.
            _domain.Clear();
            foreach (T x in timeline[0].ToCollapsedValues())
            {
                _domain.Add(x);
            }

            // Fresh pass bookkeeping
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
        /// Scans the quantum scrapbook to check if all possible futures have stopped bickering like caffeinated otters in a time loop.
        /// </summary>
        public int Converged()
        {
            // If the engine itself already flagged full convergence, we're done.
            if (_runtime.Converged)
            {
                return 1;
            }

            // We only need two slices to detect "last two equal".
            if (timeline.Count < 2)
            {
                return 0;
            }

            // If the last two slices match, that's a 1‐step convergence.
            if (SameStates(timeline[^1], timeline[^2]))
            {
                return 1;
            }

            // Otherwise look for any earlier slice that matches the last.
            for (int i = 2; i <= timeline.Count; i++)
            {
                if (SameStates(timeline[^1], timeline[^i]))
                {
                    return i - 1;
                }
            }

            // No convergence detected yet.
            return 0;
        }


        /// <summary>
        /// Unifies all timeline slices into a single collapsed state, like herding quantum cats into one box.
        /// </summary>
        public void UnifyAll()
        {
            // Nothing to do if we still only have the bootstrap.
            if (timeline.Count < 2)
            {
                return;
            }

            // Merge all values that appeared after the bootstrap (slice 0).
            T[] mergedStates = [.. timeline
                                .Skip(1)
                                .SelectMany(q => q.ToCollapsedValues())
                                .Distinct()];

            QuBit<T> unified = new(mergedStates);
            unified.Any();

            // Replace everything after the bootstrap with the unified slice.
            if (timeline.Count > 1)
            {
                timeline.RemoveRange(1, timeline.Count - 1);
            }

            // Keep epoch stamps in sync: drop stamps past bootstrap.
            if (_sliceEpochs.Count > 1)
            {
                _sliceEpochs.RemoveRange(1, _sliceEpochs.Count - 1);
            }

            timeline.Add(unified);
            StampAppendCurrentEpoch();

            // Refresh the domain to reflect the new union.
            _domain.Clear();
            foreach (T x in mergedStates)
            {
                _domain.Add(x);
            }

            _runtime.Converged = true;
            OnCollapse?.Invoke();
            OnConverged?.Invoke();

        }



        /// <summary>
        /// Unifies the last 'count' timeline slices into one, forcing alternate realities to shake hands and make up.
        /// </summary>
        public void Unify(int count)
        {
            if (count < 2 || timeline.Count < count)
            {
                return;
            }

            int start = timeline.Count - count;
            List<T> merged = [.. timeline
                .Skip(start)
                .SelectMany(qb => qb.ToCollapsedValues())
                .Distinct()];

            timeline.RemoveRange(start, count);

            // Keep epochs in sync with the removal.
            if (_sliceEpochs.Count > start)
            {
                _sliceEpochs.RemoveRange(start, _sliceEpochs.Count - start);
            }

            QuBit<T> newQb = new(merged);
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
            QuBit<T> qb = other.GetCurrentQBit();

            // Treat this as a cross-variable assignment (source = other).
            ReplaceOrAppendOrUnify(qb, replace: true, isFeedbackFromSelf: ReferenceEquals(this, other), crossSource: other);
        }



        /// <summary>
        /// Disambiguation overload so calls like <c>v.Assign(v + 1)</c> bind deterministically, because even in the multiverse, syntax matters more than you'd think.
        /// Delegates to <see cref="Assign(QuBit{T})"/>.
        /// </summary>
        public void Assign(QExpr expr)
        {
            bool isFeedbackFromSelf = ReferenceEquals(this, expr.Source);
            QuBit<T> qb = (QuBit<T>)expr; // force materialization (eager or lazy)

            // Temporal law: for cross-variable forward writes, only the SOURCE gets logged.
            // The TARGET stays unlogged here; reconstruction happens during reverse ticks.
            ReplaceOrAppendOrUnify(qb, replace: true, isFeedbackFromSelf, expr.Source);
            _sawStateReadThisForward = false;
        }
        /// <summary>
        /// Detects if we've accidentally invented time travel by repeating ourselves.
        /// </summary>
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

        // Central ingest point for writes, with optional cross-variable context.
        private void ReplaceOrAppendOrUnify(QuBit<T> qb, bool replace, bool isFeedbackFromSelf = false, PositronicVariable<T> crossSource = null)
        {
            if (InConvergenceLoop)
            {
                int e = _runtime.Entropy;
                if (e != s_LastEntropySeenForEpoch)
                {
                    BeginEpoch();
                    s_LastEntropySeenForEpoch = e;
                    ResetReverseReplayFlag();
                }
            }


            IPositronicRuntime runtime = _runtime;


            // ---------- Forward, in-loop ----------
            if (runtime.Entropy > 0 && InConvergenceLoop)
            {
                // During forward ticks, ignore cross-variable TARGET writes.
                // We only reconstitute them on reverse ticks for causality bookkeeping.
                if (!isFeedbackFromSelf && crossSource is not null)
                {
                    return;
                }

                // Self-feedback early-union (first write).
                if (replace && isFeedbackFromSelf && timeline.Count > 0
                    && !_ops.SawForwardWrite
                    && !_sawStateReadThisForward)
                {
                    T[] incoming = [.. qb.ToCollapsedValues()];
                    T[] last = [.. timeline[^1].ToCollapsedValues()];

                    bool bothScalar = incoming.Length == 1 && last.Length == 1;
                    if (bothScalar)
                    {
                        T[] unionValues = [.. last.Union(incoming).Distinct()];
                        QuBit<T> unionQb = new(unionValues); unionQb.Any();

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
                }

                // Self-feedback subsequent writes (union growth).
                if (replace && isFeedbackFromSelf && timeline.Count > 0 && _ops.SawForwardWrite)
                {
                    T[] incoming = [.. qb.ToCollapsedValues()];
                    bool lastIsNonScalar = timeline[^1].ToCollapsedValues().Skip(1).Any();

                    if (lastIsNonScalar)
                    {
                        T[] unionValues = [.. timeline[^1].ToCollapsedValues()
                                         .Union(incoming)
                                         .Distinct()];
                        QuBit<T> unionQb = new(unionValues); unionQb.Any();

                        _temporalRecords.ReplaceLastSlice(this, unionQb);
                        StampReplaceCurrentEpoch();

                        _ops.SawForwardWrite = true;
                        DetectAndUnifySmallCycle();
                        return;
                    }
                }

                // Bootstrap is sacred during the loop; first real write appends.
                if (timeline.Count == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
                    _ops.SawForwardWrite = true;
                    DetectAndUnifySmallCycle();
                    return;
                }

                // Normal append/replace for non-scalar writes.
                if (replace)
                {
                    T[] incoming = [.. qb.ToCollapsedValues()];
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

                // Scalar (replace == false) — if we already wrote this half-cycle, REPLACE; otherwise APPEND.
                if (_ops.SawForwardWrite && timeline.Count > 0)
                {
                    _temporalRecords.ReplaceLastSlice(this, qb);
                    StampReplaceCurrentEpoch();
                }
                else
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
                }

                T[] valsForBaseline = [.. qb.ToCollapsedValues().Take(2)];
                if (valsForBaseline.Length == 1 && crossSource is null)
                {
                    _forwardScalarBaseline = valsForBaseline[0];
                    _hasForwardScalarBaseline = true;
                }

                _ops.SawForwardWrite = true;
                DetectAndUnifySmallCycle();
                return;
            }


            // ---------- Reverse, in-loop ----------
            if (runtime.Entropy < 0 && InConvergenceLoop)
            {
                // If the last forward used Required for this variable, do NOT run reverse replay for it.
                // We want the pre-read slice to match the narrowed state, not to be k-shifted by print-site ops.
                if (_hadRequiredThisForward)
                {
                    AppendFromReverse(qb);              // append the cloned current slice as-is (no replay)
                    _hadRequiredThisForward = false;    // consume flag
                    return;
                }

                // Cross-variable assignment: reconstruct TARGET locally without consuming replay from SOURCE.
                if (!isFeedbackFromSelf && crossSource is not null)
                {
                    bool alreadyRebuiltThisEpoch =
                        _sliceEpochs.Count > 0
                        && _sliceEpochs[^1] == CurrentEpoch
                        && timeline.Count == _sliceEpochs.Count;

                    if (alreadyRebuiltThisEpoch)
                    {
                        return;
                    }

                    T[] incomingVals = [.. qb.ToCollapsedValues()];
                    T[] srcVals = [.. crossSource.GetCurrentQBit().ToCollapsedValues()];

                    // Did this TARGET receive a closing scalar in the last forward half-cycle?
                    bool hasClosingScalar = _hasForwardScalarBaseline;

                    QuBit<T> rebuilt;

                    if (!hasClosingScalar)
                    {
                        // No scalar close on TARGET: adopt the incoming expression result as-is.
                        rebuilt = new QuBit<T>(incomingVals);
                        rebuilt.Any();

                        // Replace within epoch if we already rebuilt once; otherwise keep a single latest slice.
                        if (_sliceEpochs.Count > 0 && _sliceEpochs[^1] == CurrentEpoch && timeline.Count == _sliceEpochs.Count)
                        {
                            ReplaceLastFromReverse(rebuilt);
                        }
                        else
                        {
                            ReplaceForwardHistoryWith(rebuilt);
                        }

                        return;
                    }

                    // TARGET had a scalar close → compute baseline shift k = incoming − src, and apply to target’s forward baseline.
                    if (incomingVals.Length == 1 && srcVals.Length == 1)
                    {
                        dynamic incoming = incomingVals[0];
                        dynamic src = srcVals[0];
                        dynamic k = incoming - src;

                        dynamic baseVal;
                        if (_hasForwardScalarBaseline)
                        {
                            baseVal = _forwardScalarBaseline;
                        }
                        else
                        {
                            T[] lastTargetVals = timeline.Count > 0 ? [.. timeline[^1].ToCollapsedValues()] : [];
                            baseVal = lastTargetVals.Length == 1 ? lastTargetVals[0] : default;
                        }

                        try
                        {
                            T shifted = (T)(baseVal + k);
                            rebuilt = new QuBit<T>(new[] { shifted });
                            rebuilt.Any();
                        }
                        catch
                        {
                            rebuilt = new QuBit<T>(incomingVals);
                            rebuilt.Any();
                        }
                    }
                    else
                    {
                        rebuilt = new QuBit<T>(incomingVals);
                        rebuilt.Any();
                    }

                    // Keep only a single latest reconstructed slice beyond the bootstrap.
                    ReplaceForwardHistoryWith(rebuilt);
                    return;
                }


                if (ReferenceEquals(qb, timeline[^1]))
                {
                    qb = new QuBit<T>(qb.ToCollapsedValues().ToArray());
                }

                IOperation top = QuantumLedgerOfRegret.Peek();
                if (top is null or MerlinFroMarker)
                {
                    return;
                }

                QuBit<T> rebuiltx = _reverseReplay.ReplayReverseCycle(qb, this);

                if (isFeedbackFromSelf && timeline.Count > 0)
                {
                    T[] union = [.. rebuiltx.ToCollapsedValues().Union(timeline[^1].ToCollapsedValues()).Distinct()];
                    rebuiltx = new QuBit<T>(union).Any();
                }

                AppendFromReverse(rebuiltx);
                return;
            }

            // ---------- Forward, outside the loop ----------
            // Emergency override for when we're flailing outside the loop like a time-traveling otter.
            if (runtime.Entropy > 0 && !InConvergenceLoop)
            {
                // First ever forward write OR first after Unify → APPEND.
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
                    IEnumerable<T> lastStates = timeline[^1].ToCollapsedValues();
                    T[] merged = [.. lastStates
                           .Union(qb.ToCollapsedValues())
                           // Only pull the bootstrap into the union when the last slice
                           // still represents a single scalar   (guarantees _pure_ scalar sequence)
                           .Union(lastStates.Count() == 1 ? timeline[0].ToCollapsedValues() : [])
                           .Distinct()];
                    QuBit<T> mergedQb = new(merged); mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    StampReplaceCurrentEpoch();
                    _hadOutsideWritesSinceLastLoop = true;
                }
                else
                {
                    // Preserve causality, or Marty McFly fades again.
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
                    _ops.SawForwardWrite = true;
                    _hadOutsideWritesSinceLastLoop = true;
                }

                return;
            }

            // If we've already reached enlightenment, stop messing with the timeline.
            if (_runtime.Converged)
            {
                return;
            }

            Remember(qb.ToCollapsedValues());

            // ---------- Reverse, outside the loop ----------
            if (runtime.Entropy < 0)
            {
                // Cross-variable outside-loop: rebuild locally and replace (don't mutate the source, don't duplicate slices).
                if (crossSource is not null && !isFeedbackFromSelf)
                {
                    T[] incomingVals = [.. qb.ToCollapsedValues()];
                    T[] srcVals = [.. crossSource.GetCurrentQBit().ToCollapsedValues()];
                    T[] lastTargetVals = timeline.Count > 0 ? [.. timeline[^1].ToCollapsedValues()] : [];

                    QuBit<T> rebuilt;
                    if (incomingVals.Length == 1 && srcVals.Length == 1 && lastTargetVals.Length == 1)
                    {
                        dynamic incoming = incomingVals[0];
                        dynamic src = srcVals[0];
                        dynamic last = lastTargetVals[0];

                        dynamic k = incoming - src;
                        try
                        {
                            T shifted = (T)(last + k);
                            QuBit<T> q = new([shifted]); q.Any();
                            rebuilt = q;
                        }
                        catch
                        {
                            QuBit<T> q = new(incomingVals); q.Any();
                            rebuilt = q;
                        }
                    }
                    else
                    {
                        QuBit<T> q = new(incomingVals); q.Any();
                        rebuilt = q;
                    }

                    // Outside loop: keep a single rebuilt slice beyond bootstrap.
                    ReplaceForwardHistoryWith(rebuilt);
                    OnTimelineAppended?.Invoke();
                    return;
                }

                IOperation top = QuantumLedgerOfRegret.Peek();
                if (top is null or MerlinFroMarker)
                {
                    return;
                }

                // True reverse replay for self-feedback or same-variable.
                QuBit<T> rebuiltStd = _reverseReplay.ReplayReverseCycle(qb, this);
                AppendFromReverse(rebuiltStd);
                OnTimelineAppended?.Invoke();
                return;
            }

            // ---------- Forward, generic ----------
            if (runtime.Entropy > 0)
            {
                // Overwrite bootstrap if that's all we have.
                if (bootstrapSuperseded && timeline.Count == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    return;
                }

                // First real forward write: snapshot+append.
                if (!bootstrapSuperseded && timeline.Count == 1)
                {
                    _temporalRecords.SnapshotAppend(this, qb);
                    _ops.SawForwardWrite = true;
                    bootstrapSuperseded = true;
                    return;
                }

                // Overwrite (merge) if this is the very first forward write this epoch.
                if (replace && !_ops.SawForwardWrite)
                {
                    T[] merged = [.. timeline[^1]
                       .ToCollapsedValues()
                       .Union(qb.ToCollapsedValues())
                       .Distinct()];
                    QuBit<T> mergedQb = new(merged);
                    mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    StampReplaceCurrentEpoch();
                    _ops.SawForwardWrite = true;
                    return;
                }
                // Otherwise decide append vs. merge.
                if (!replace || (!_ops.SawForwardWrite && !SameStates(qb, timeline[^1])))
                {
                    // Snapshot-append the new slice.
                    _temporalRecords.SnapshotAppend(this, qb);
                    StampAppendCurrentEpoch();
                    _ops.SawForwardWrite = true;
                }
                else
                {
                    // Merge into the last slice if it's a duplicate scalar write.
                    T[] merged = [.. timeline[^1]
                                          .ToCollapsedValues()
                                          .Union(qb.ToCollapsedValues())
                                          .Distinct()];
                    QuBit<T> mergedQb = new(merged);
                    mergedQb.Any();
                    _temporalRecords.ReplaceLastSlice(this, mergedQb);
                    StampReplaceCurrentEpoch();
                }


                // Detect any small cycle and unify.
                for (int cycle = 2; cycle <= 20; cycle++)
                {
                    if (timeline.Count >= cycle + 1 && SameStates(timeline[^1], timeline[^(cycle + 1)]))
                    {
                        Unify(cycle);
                        break;
                    }
                }

                return;
            }

        }

        #region region Operator Overloads
        // --- Operator Overloads ---
        // --- Addition Overloads ---
        public static QExpr operator +(PositronicVariable<T> left, T right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = left.GetCurrentQBit();
                QuBit<T> resultQB = srcQB + right;
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, lazy);
        }

        public static QExpr operator +(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() + left;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator +(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- QExpr-aware Addition Overloads ---
        public static QExpr operator +(PositronicVariable<T> left, QExpr right)
        {
            // Materialize the right expression's qubit; do not log here (ops already logged when building 'right')
            QuBit<T> resultQB = left.GetCurrentQBit() + (QuBit<T>)right;
            MarkAnyIfSuperposition(resultQB);
            return new QExpr(left, resultQB);
        }

        public static QExpr operator +(QExpr left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = (QuBit<T>)left + right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            // Use left.Source as the QExpr's source
            return new QExpr(left.Source, resultQB);
        }

        // --- Subtraction Overloads ---
        public static QExpr operator -(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() - right;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator -(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() - left;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator -(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Unary Negation ---
        public static QExpr operator -(PositronicVariable<T> value)
        {
            QuBit<T> qb = value.GetCurrentQBit();
            T[] negatedValues = [.. qb.ToCollapsedValues().Select(v => { dynamic dv = v; return (T)(-dv); })];
            QuBit<T> negatedQb = new(negatedValues);
            MarkAnyIfSuperposition(negatedQb);

            return new QExpr(value, negatedQb);
        }

        // --- Multiplication Overloads ---

        public static QExpr operator *(PositronicVariable<T> left, T right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = left.GetCurrentQBit();
                QuBit<T> resultQB = srcQB * right;
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }

            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(
                    IsMinus1(right)
                        ? new NegationOperation<T>(left)
                        : new MultiplicationOperation<T>(left, right));
            }
            return new QExpr(left, lazy);
        }

        public static QExpr operator *(T left, PositronicVariable<T> right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = right.GetCurrentQBit();
                T[] multiplied = [.. srcQB
                    .ToCollapsedValues()
                    .Select(v => { dynamic dl = left; dynamic dv = v; return (T)(dl * dv); })
                    .Distinct()];
                QuBit<T> resultQB = new(multiplied);
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }

            if (!s_SuppressOperatorLogging && right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(
                    IsMinus1(left)
                        ? new NegationOperation<T>(right)
                        : new MultiplicationOperation<T>(right, left));
            }
            return new QExpr(right, lazy);
        }

        // --- Division Overloads ---
        public static QExpr operator /(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() / right;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator /(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() / left;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator /(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() / right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Modulus Overloads ---
        public static QExpr operator %(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() % right;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator %(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() % left;
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && right._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator %(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            T divisor = right.GetCurrentQBit().ToCollapsedValues().First();
            QuBit<T> resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!s_SuppressOperatorLogging && left._runtime.Entropy >= 0)
            {
                QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Comparison operators (Comparer<T>.Default) riding shotgun ---

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
            return ReferenceEquals(left, right) || (left is not null && right is not null && left.SameStates(left.GetCurrentQBit(), right.GetCurrentQBit()));
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
            return obj is PositronicVariable<T> other && this == other;
        }

        public override int GetHashCode()
        {
            return GetCurrentQBit().ToCollapsedValues().Aggregate(0, (acc, x) => acc ^ (x?.GetHashCode() ?? 0));
        }

        /// <summary>
        /// Retrieves a specific slice from the timeline, like flipping to a particular episode in your life's never-ending sitcom.
        /// </summary>
        public QuBit<T> GetSlice(int stepsBack)
        {
            return stepsBack < 0 || stepsBack >= timeline.Count
                ? throw new ArgumentOutOfRangeException(nameof(stepsBack))
                : timeline[timeline.Count - 1 - stepsBack];
        }

        /// <summary>
        /// Returns all timeline slices in order, for when you need to relive every awkward moment.
        /// </summary>
        public IEnumerable<QuBit<T>> GetTimeline()
        {
            return timeline;
        }

        /// <summary>
        /// Folds the last known state into a reality burrito and pretends none of the branching ever happened.
        /// </summary>
        public void CollapseToLastSlice()
        {
            QuBit<T> last = timeline.Last();
            T baseline = last.ToCollapsedValues().First();
            QuBit<T> collapsedQB = new([baseline]);
            collapsedQB.Any();
            timeline.Clear();
            timeline.Add(collapsedQB);
            OnCollapse?.Invoke();
        }

        /// <summary>
        /// Marks the qubit as "any" if it is still in superposition.
        /// </summary>
        /// <param name="qb"></param>
        private static void MarkAnyIfSuperposition(QuBit<T> qb)
        {
            if (qb.GetCurrentType() != QuantumSuperposition.Core.QuantumStateType.CollapsedResult)
            {
                qb.Any();
            }
        }

        /// <summary>
        /// Collapses the timeline using a custom strategy, like choosing your own adventure in a book of infinite pages.
        /// </summary>
        public void CollapseToLastSlice(Func<IEnumerable<T>, T> strategy)
        {
            QuBit<T> last = timeline.Last();
            T chosenValue = strategy(last.ToCollapsedValues());
            QuBit<T> collapsedQB = new([chosenValue]);
            collapsedQB.Any();
            timeline.Clear();
            timeline.Add(collapsedQB);
            OnCollapse?.Invoke();
        }

        // --- Built-in collapse strategies ---
        public Func<IEnumerable<T>, T> CollapseMin = values => values.Min();
        public Func<IEnumerable<T>, T> CollapseMax = values => values.Max();
        public Func<IEnumerable<T>, T> CollapseFirst = values => values.First();
        public Func<IEnumerable<T>, T> CollapseRandom = values =>
        {
            List<T> list = [.. values];
            Random rnd = new();
            return list[rnd.Next(list.Count)];
        };

        /// <summary>
        /// Creates a new branch by forking the timeline, spawning a parallel universe where you finally clean your room.
        /// </summary>
        public PositronicVariable<T> Fork(IPositronicRuntime runtime)
        {
            List<QuBit<T>> forkedTimeline = timeline.Select(qb =>
            {
                QuBit<T> newQB = new(qb.ToCollapsedValues().ToArray());
                newQB.Any();
                return newQB;
            }).ToList();

            PositronicVariable<T> forked = new(forkedTimeline[0], runtime);
            forked.timeline.Clear();
            forkedTimeline.ForEach(forked.timeline.Add);
            return forked;
        }

        /// <summary>
        /// Forks the timeline and applies a transformation to the final slice, like editing the ending of your autobiography.
        /// </summary>
        public PositronicVariable<T> Fork(Func<T, T> transform, IPositronicRuntime runtime)
        {
            PositronicVariable<T> forked = Fork(runtime);
            QuBit<T> last = forked.timeline.Last();
            T[] transformedValues = last.ToCollapsedValues().Select(transform).ToArray();
            forked.timeline[^1] = new QuBit<T>(transformedValues);
            forked.timeline[^1].Any();
            return forked;
        }

        /// <summary>
        /// Emits a readable account of all the timeline's identity crises.
        /// Good for debugging. Also good for therapists.
        /// </summary>
        public string ToTimelineString()
        {
            return string.Join(Environment.NewLine,
                timeline.Select((qb, index) => $"Slice {index}: {qb}"));
        }

        /// <summary>
        /// Serializes the timeline to JSON, why would you not? Good for debugging, but not good for your therapist bill.
        /// </summary>
        public string ExportToJson()
        {
            JsonSerializerOptions options = new() { WriteIndented = true };
            return JsonSerializer.Serialize(timeline, options);
        }

        /// <summary>
        /// Exposes the current quantum values like a flasher in the multiverse.
        /// </summary>
        public PositronicValueWrapper Value => new(GetCurrentQBit());

        public class PositronicValueWrapper
        {
            private readonly QuBit<T> qb;
            public PositronicValueWrapper(QuBit<T> q)
            {
                qb = q;
            }

            public IEnumerable<T> ToValues()
            {
                return qb.ToCollapsedValues();
            }
        }

        public void ConstrainEqual(QExpr expr)
        {
            ArgumentNullException.ThrowIfNull(expr.Source);

            QuBit<T> qb;

            // Suppress log while materializing a self-sourced constraint, so building the QExpr
            // doesn't push addition/mul ops for this variable.
            if (ReferenceEquals(this, expr.Source))
            {
                s_SuppressOperatorLogging = true;
                try
                {
                    qb = (QuBit<T>)expr;
                }
                finally
                {
                    s_SuppressOperatorLogging = false;
                }

                if (InConvergenceLoop && _runtime.Entropy > 0)
                {
                    QuantumLedgerOfRegret.Record(new MerlinFroMarker());
                }
            }
            else
            {
                qb = (QuBit<T>)expr;
            }

            ConstrainEqual(qb);
        }

        /// <summary>
        /// Narrows this variable to be equal to the given state (equality constraint), like forcing a stubborn cat into a smaller box.
        /// Replaces the most recent slice (or appends if only bootstrap exists). No union growth.
        /// </summary>
        public void ConstrainEqual(QuBit<T> qb)
        {
            ArgumentNullException.ThrowIfNull(qb);

            lock (_tvarLock)
            {
                if (InConvergenceLoop)
                {
                    int e = _runtime.Entropy;
                    if (e != s_LastEntropySeenForEpoch)
                    {
                        BeginEpoch();
                        s_LastEntropySeenForEpoch = e;
                        ResetReverseReplayFlag();
                    }
                }

                if (_runtime.Entropy > 0)
                {
                    if (timeline.Count <= 1)
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
                    _hadRequiredThisForward = true;
                }
                else if (_runtime.Entropy < 0)
                {
                    AppendFromReverse(qb);
                }
                else
                {
                    if (timeline.Count <= 1)
                    {
                        _temporalRecords.SnapshotAppend(this, qb);
                        StampAppendCurrentEpoch();
                    }
                    else
                    {
                        _temporalRecords.ReplaceLastSlice(this, qb);
                        StampReplaceCurrentEpoch();
                    }
                }

                Interlocked.Increment(ref _tvarVersion);
            }
        }

        /// <summary>
        /// Gets or sets the current quantum state, because sometimes you need to peek behind the curtain of reality.
        /// Returns a QExpr to ensure arithmetic operations are logged correctly.
        /// </summary>
        public QExpr State
        {
            get
            {
                if (InConvergenceLoop && _runtime.Entropy > 0)
                {
                    _sawStateReadThisForward = true;
                }

                return new QExpr(this, () =>
                {
                    QuBit<T> qb = GetCurrentQBit();

                    return qb;
                });

            }
            set => ConstrainEqual(value);
        }

        /// <summary>
        /// Does it exist or not? Schrödinger would be proud.
        /// </summary>
        public T Scalar
        {
            get => GetCurrentQBit().ToCollapsedValues().First();
            set => Assign(value);
        }


        /// <summary>
        /// Forces the quantum superposition to pick a side.
        /// Like asking a cat to choose a political party.
        /// </summary>
        public IEnumerable<T> ToValues()
        {
            return GetCurrentQBit().ToCollapsedValues();
        }

        /// <summary>
        /// Retrieves the freshest slice of existence, still warm from the cosmic oven.
        /// Return the "current" qubit:
        ///  - outside the loop: last slice wins (legacy behavior)
        ///  - inside the loop : last slice of the current epoch (if tagged), else newest slice
        /// </summary>
        public QuBit<T> GetCurrentQBit()
        {
            if (timeline.Count == 0)
            {
                throw new InvalidOperationException("Timeline is empty.");
            }

            if (!InConvergenceLoop)
            {
                return timeline[^1];
            }

            // Inside the convergence loop: drift to the most recent slice stamped for this epoch.
            int epoch = CurrentEpoch;
            // Defensive: avoid stepping out the airlock if tags are short.
            int lastStamped = Math.Min(timeline.Count, _sliceEpochs.Count) - 1;
            for (int i = lastStamped; i >= 0; i--)
            {
                if (_sliceEpochs[i] == epoch)
                {
                    return timeline[i];
                }
            }
            // Fallback when tags are missing or out of sync (e.g., after Unify): newest slice wins.
            return timeline[^1];

        }


        /// <summary>
        /// Compares two quantum blobs to see if they're secretly the same blob in a hat.
        /// </summary>
        private bool SameStates(QuBit<T> a, QuBit<T> b)
        {
            List<T> av = a.ToCollapsedValues().OrderBy(x => x).ToList();
            List<T> bv = b.ToCollapsedValues().OrderBy(x => x).ToList();
            if (av.Count != bv.Count)
            {
                return false;
            }

            for (int i = 0; i < av.Count; i++)
            {
                if (!Equals(av[i], bv[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return GetCurrentQBit().ToString();
        }

        internal void AppendCloneOfCurrentForReverseTick()
        {
            // Ensure the reverse half-cycle gets its own epoch stamp before we append.
            if (InConvergenceLoop)
            {
                int e = _runtime.Entropy;
                if (e != s_LastEntropySeenForEpoch)
                {
                    BeginEpoch();
                    s_LastEntropySeenForEpoch = e;
                    ResetReverseReplayFlag();
                }
            }

            QuBit<T> last = GetCurrentQBit();
            QuBit<T> copy = new(last.ToCollapsedValues().ToArray());
            copy.Any();

            timeline.Add(copy);
            StampAppendCurrentEpoch();
            OnTimelineAppended?.Invoke();
        }

        /// <summary>
        /// Flushes the existential junk drawer and repopulates it with what we currently believe to be true.
        /// </summary>
        internal void ResetDomainToCurrent()
        {
            _domain.Clear();
            foreach (T x in GetCurrentQBit().ToCollapsedValues())
            {
                _domain.Add(x);
            }
        }

        /// <summary>
        /// Marks this variable as having had outside-loop forward writes, like tagging graffiti on the walls of time.
        /// On the next RunConvergenceLoop(), ResetTimelineIfOutsideWrites() will quarantine to the bootstrap slice. This does NOT mutate the timeline.
        /// </summary>
        public void NoteOutsideWrites()
        {
            _hadOutsideWritesSinceLastLoop = true;
        }

        public QExpr Project(Func<T, T> projector)
        {
            ArgumentNullException.ThrowIfNull(projector);
            QuBit<T> lazy()
            {
                QuBit<T> qb = GetCurrentQBit();
                return qb.Select(projector);
            }
            return new QExpr(this, lazy);
        }

        public static QExpr Project(PositronicVariable<T> src, Func<T, T> projector)
        {
            return src.Project(projector);
        }
        // ---------- PositronicVariable<T> ⊗ QuBit<T> / QExpr ----------
        /// <summary>
        /// This is not a bitwise shift; it's a ceremonial grafting of a QuBit onto a PositronicVariable.
        /// </summary>
        public static QExpr operator <<(PositronicVariable<T> target, QuBit<T> qb)
        {
            return new QExpr(target, qb);
        }

        /// <summary>
        /// This is not a bitwise shift; it's a ceremonial grafting of a QExpr onto a PositronicVariable.
        /// </summary>
        public static QExpr operator <<(PositronicVariable<T> target, QExpr expr)
        {
            return new QExpr(target, () => (QuBit<T>)expr);
        }

        /// <summary>
        /// Proposes a new quantum state for this variable (assignment), like suggesting a plot twist to the author of the universe.
        /// </summary>
        public QExpr Proposed
        {
            get
            {
                if (InConvergenceLoop && _runtime.Entropy > 0)
                {
                    _sawStateReadThisForward = true;
                }
                return new QExpr(this, () =>
                {
                    QuBit<T> qb = GetCurrentQBit();
                    return qb;
                });
            }
            set => Assign(value);
        }

        /// <summary>
        /// Demands a new quantum state for this variable (equality constraint), because sometimes the universe needs a firm hand.
        /// </summary>
        public QExpr Required
        {
            get
            {
                if (InConvergenceLoop && _runtime.Entropy > 0)
                {
                    _sawStateReadThisForward = true;
                }
                return new QExpr(this, () =>
                {
                    QuBit<T> qb = GetCurrentQBit();
                    return qb;
                });
            }
            set => ConstrainEqual(value);
        }

        /// <summary>
        /// Run an action under a coarse-grained global transaction.
        /// Stage 1 API to help tests serialize multivariable mutations.
        /// </summary>
        public static void InTransaction(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Transaction.Run(_ => action());
        }
    }
}
