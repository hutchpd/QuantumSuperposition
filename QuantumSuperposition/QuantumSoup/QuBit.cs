using System.Numerics;
using System.Text.Json;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using QuantumSuperposition.Utilities;

namespace QuantumSuperposition.QuantumSoup
{
    public partial class QuBit<T> : QuantumSoup<T>, IQuantumReference
    {
        private readonly Func<T, bool> _valueValidator;

        private readonly int[] _qubitIndices;
        private readonly QuantumSystem? _system;
        private bool _isCollapsedFromSystem;
        private object? _systemObservedValue;
        public QuantumSystem? System => _system;

        private Guid? _entanglementGroupId;
        public Guid? EntanglementGroupId => _entanglementGroupId;
        public void SetEntanglementGroup(Guid id) => _entanglementGroupId = id;

        public bool IsInSuperposition => _eType == QuantumStateType.SuperpositionAny && !_isActuallyCollapsed;

        #region Constructors
        // Constructors that enable you to manifest chaotic energy into a typed container.
        // Also known as: Creating a mess in a mathematically defensible way.


        // A simpler constructor for the entangled case:

        public QuBit(QuantumSystem system, int[] qubitIndices)

        {

            // If T = (int,bool) then qubitIndices might be new[] {0,1}.
            // If T = int only, maybe qubitIndices = new[] {0}, etc.

            _system = system;
            _qubitIndices = qubitIndices ?? Array.Empty<int>();
            _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);

            // I exist yelled the qubit, and the system is my parent! Daddy!!!
            _system.Register(this);

            _qList = new List<T> { (T)Convert.ChangeType(0, typeof(T)), (T)Convert.ChangeType(1, typeof(T)) };

            // This qubit is in superposition until a collapse occurs
            _eType = QuantumStateType.SuperpositionAny;

        }

        /// <summary>
        /// If we have no quantum system, we fallback to local states:
        /// </summary>
        /// <param name="Items"></param>
        /// <param name="ops"></param>
        /// <param name="valueValidator"></param>
        public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
            : this(Items, ops)
        {
            _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
        }

        public QuBit(IEnumerable<T> Items, Func<T, bool>? valueValidator = null)
            : this(Items, _defaultOps, valueValidator)
        { }

        public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
            : this(weightedItems, ops)
        {
            _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
        }

        public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, Func<T, bool>? valueValidator = null)
            : this(weightedItems, _defaultOps, valueValidator)
        { }

        internal QuBit(IEnumerable<T> items, Dictionary<T, Complex>? weights, IQuantumOperators<T> ops, Func<T, bool>? valueValidator = null)
            : this(items, weights, ops)
        {
            _valueValidator = valueValidator ?? (v => !EqualityComparer<T>.Default.Equals(v, default));
        }

        /// <summary>
        /// Applies a quantum conditional — like an if-else, but across all timelines.
        /// Each branch of the superposition is checked with a predicate.
        /// If the predicate is true, the <paramref name="ifTrue"/> function is applied to that branch;
        /// otherwise, <paramref name="ifFalse"/> is applied.
        /// 
        /// The resulting states are merged, and their amplitudes are weighted accordingly.
        /// No collapse occurs. Nobody gets observed. Reality remains deeply confused.
        /// </summary>
        /// <param name="predicate">
        /// A function that decides whether a given branch deserves to go down the happy path. Think Loki season 2 but with weightings.
        /// </param>
        /// <param name="ifTrue">
        /// Transformation applied to branches that satisfy the predicate.
        /// Think of this as the "yes, and..." timeline.
        /// </param>
        /// <param name="ifFalse">
        /// Transformation for the other branches — the "meh, fine" timeline.
        /// </param>
        /// <returns>
        /// A new QuBit that merges the transformed branches into one beautifully indecisive superposition.
        /// </returns>
        public QuBit<T> Conditional(
            Func<T, Complex, bool> weightedPredicate,
            Func<QuBit<T>, QuBit<T>> ifTrue,
            Func<QuBit<T>, QuBit<T>> ifFalse)
        {
            var newWeights = new Dictionary<T, Complex>();
            var newStates = new List<T>();

            foreach (var (value, weight) in ToWeightedValues())
            {
                // Create a branch qubit for the current state with its weight
                var branchQubit = new QuBit<T>(new[] { value }, this.Operators)
                                  .WithWeights(new Dictionary<T, Complex> { { value, weight } }, autoNormalise: false);

                // Use the weight-aware predicate to choose the branch function.
                // The predicate now receives the current state value and its weight.
                QuBit<T> mappedBranch = weightedPredicate(value, weight)
                    ? ifTrue(branchQubit)
                    : ifFalse(branchQubit);

                // When recombining, multiply the original branch weight with the branch’s transformation weight.
                foreach (var (mappedValue, mappedWeight) in mappedBranch.ToWeightedValues())
                {
                    Complex combinedWeight = weight * mappedWeight;
                    if (newWeights.ContainsKey(mappedValue))
                        newWeights[mappedValue] += combinedWeight;
                    else
                    {
                        newWeights[mappedValue] = combinedWeight;
                        newStates.Add(mappedValue);
                    }
                }
            }

            return new QuBit<T>(newStates, newWeights, this.Operators);
        }

        /// <summary>
        /// Applies a transformation to every possible state in the qubit — like a map,
        /// but across all realities at once.
        ///
        /// Each branch is run through the <paramref name="selector"/> function, producing
        /// a new set of possibilities in a parallel type universe. The original quantum
        /// amplitudes (weights) are preserved, because we care about continuity.
        ///
        /// No collapse happens. No commitment is made. The universe remains beautifully noncommittal.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type each original state gets transformed into. Like evolving your indecision into a new, equally undecided form.
        /// </typeparam>
        /// <param name="selector">
        /// A function that transforms a state from T to TResult, without actually forcing it to pick one.
        /// </param>
        /// <returns>
        /// A new QuBit<TResult> holding the transformed superposition,
        /// complete with its inherited existential probabilities.
        /// </returns>
        public QuBit<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            // This is the quantum equivalent of "map", but without picking a side.
            // Each state gets passed through the selector, but their identity crisis (weight) stays intact.
            var mappedWeightedValues = this.ToWeightedValues()
                .Select(pair => (value: selector(pair.value), weight: pair.weight));

            // The new qubit lives in a different type universe now, so we find its corresponding math handlers.
            var newOps = QuantumOperatorsFactory.GetOperators<TResult>();

            // Create a new superposition in the new type space.
            // Note: nothing actually happens until someone observes this — classic quantum laziness.
            return new QuBit<TResult>(mappedWeightedValues, newOps);
        }

        /// <summary>
        /// Projects each quantum branch into a new qubit and flattens the result,
        /// producing a tangled multiverse of possibilities with inherited probabilities.
        ///
        /// Think of it like quantum fanfiction: every state gets its own spinoff series,
        /// and we stitch them all together into one tangled narrative.
        /// </summary>
        /// <typeparam name="TResult">
        /// The resulting type of each new quantum reality after transformation.
        /// </typeparam>
        /// <param name="selector">
        /// A function that takes a single state and returns a whole new qubit. That’s right,
        /// each state gets to live its best alternate life.
        /// </param>
        /// <returns>
        /// A new <see cref="QuBit{TResult}"/> representing all possible child states,
        /// appropriately weighted by quantum guilt (a.k.a. amplitude multiplication).
        /// </returns>
        public QuBit<TResult> SelectMany<TResult>(Func<T, QuBit<TResult>> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            IEnumerable<(TResult value, Complex weight)> Combined()
            {
                // For each weighted state in the current qubit...
                foreach (var (outerValue, outerWeight) in ToWeightedValues())
                {
                    // ...get the resulting qubit from the selector.
                    var innerQuBit = selector(outerValue);
                    // Multiply the outer weight by each inner weight.
                    foreach (var (innerValue, innerWeight) in innerQuBit.ToWeightedValues())
                        yield return (innerValue, outerWeight * innerWeight);
                }
            }

            return new QuBit<TResult>(Combined(), QuantumOperatorsFactory.GetOperators<TResult>());
        }

        /// <summary>
        /// Projects each quantum state into a new qubit, then combines the original and
        /// resulting states into a final value using a result selector —
        /// like a cosmic buddy-cop movie across entangled timelines.
        ///
        /// This is the monadic version of “yes, and…”
        /// </summary>
        /// <typeparam name="TResult">
        /// The intermediate state produced by the selector.
        /// </typeparam>
        /// <typeparam name="TResult2">
        /// The final projected type, formed by combining the outer and inner results.
        /// </typeparam>
        /// <param name="selector">
        /// A function that transforms each original value into a new qubit.
        /// </param>
        /// <param name="resultSelector">
        /// A function that combines the outer and inner values into something beautiful and final.
        /// </param>
        /// <returns>
        /// A <see cref="QuBit{TResult2}"/> representing the fused aftermath of all superposed transformations.
        /// </returns>
        public QuBit<TResult2> SelectMany<TResult, TResult2>(
            Func<T, QuBit<TResult>> selector,
            Func<T, TResult, TResult2> resultSelector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            IEnumerable<(TResult2 value, Complex weight)> Combined()
            {
                foreach (var (outerValue, outerWeight) in ToWeightedValues())
                {
                    var innerQuBit = selector(outerValue);
                    foreach (var (innerValue, innerWeight) in innerQuBit.ToWeightedValues())
                    {
                        // Combine the outer and inner values via the result selector.
                        yield return (resultSelector(outerValue, innerValue), outerWeight * innerWeight);
                    }
                }
            }

            return new QuBit<TResult2>(Combined(), QuantumOperatorsFactory.GetOperators<TResult2>());
        }

        /// <summary>
        /// Filters the multiverse down to only those branches that satisfy your criteria —
        /// a little quantum Marie Kondo moment.
        ///
        /// Any state that doesn’t spark joy (or pass the predicate) is quietly discarded into
        /// the void. Their amplitudes will not be missed.
        /// </summary>
        /// <param name="predicate">
        /// A function used to judge the life choices of each possible state.
        /// </param>
        /// <returns>
        /// A new <see cref="QuBit{T}"/> containing only the morally and mathematically acceptable states.
        /// </returns>
        public QuBit<T> Where(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            // Lazy iterator filtering the weighted states:
            IEnumerable<(T value, Complex weight)> Filter()
            {
                foreach (var (value, weight) in this.ToWeightedValues())
                {
                    if (predicate(value))
                        yield return (value, weight);
                }
            }

            return new QuBit<T>(Filter(), this.Operators);
        }


        public static QuBit<T> Superposed(IEnumerable<T> states)
        {
            var qubit = new QuBit<T>(states);
            var distinctCount = qubit.States.Distinct().Count();
            if (distinctCount > 1)
                qubit._eType = QuantumStateType.SuperpositionAny;
            return qubit;
        }

        // Main constructor for unweighted items (local useage)
        public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops)
        {
            if (Items == null) throw new ArgumentNullException(nameof(Items));
            _ops = ops ?? throw new ArgumentNullException(nameof(ops));
            _qList = Items;

            if (_qList.Distinct().Count() > 1)
                _eType = QuantumStateType.SuperpositionAny;

            // no system
            _system = null;
            _qubitIndices = Array.Empty<int>();
            _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
        }

        public QuBit(IEnumerable<T> Items)
            : this(Items, _defaultOps)
        { }


        public QuBit(IEnumerable<(T value, Complex weight)> weightedItems)
            : this(weightedItems, _defaultOps)
        { }

        public QuBit(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops)
        {
            if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
            _ops = ops ?? throw new ArgumentNullException(nameof(ops));

            var dict = new Dictionary<T, Complex>();
            foreach (var (val, w) in weightedItems)
            {
                if (!dict.ContainsKey(val))
                    dict[val] = Complex.Zero;
                dict[val] += w;
            }
            _weights = dict;
            _qList = dict.Keys; // keep a fallback list of keys

            if (_weights.Count > 1)
                SetType(QuantumStateType.SuperpositionAny);

            // no system
            _system = null;
            _qubitIndices = Array.Empty<int>();
            _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
        }

        internal QuBit(IEnumerable<T> items, Dictionary<T, Complex>? weights, IQuantumOperators<T> ops)
        {
            _qList = items;
            _weights = weights;
            _ops = ops;

            if (States.Distinct().Count() > 1)
                SetType(QuantumStateType.SuperpositionAny);

            // no system
            _system = null;
            _qubitIndices = Array.Empty<int>();
            _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
        }


        #endregion


        #region State Type Helpers

        private int[] GetUnionOfGroupIndices()
        {
            if (_system == null)
                return _qubitIndices;
            var union = new HashSet<int>(_qubitIndices);
            // Get all groups that this qubit belongs to.
            var groups = _system.Entanglement.GetGroupsForReference(this);
            foreach (var groupId in groups)
            {
                // For each qubit in each group, add its indices.
                foreach (var q in _system.Entanglement.GetGroup(groupId))
                {
                    union.UnionWith(q.GetQubitIndices());
                }
            }
            return union.OrderBy(i => i).ToArray();
        }


        public QuBit<T> Append(T element)
        {
            EnsureMutable();

            if (_weights != null)
            {
                if (!_weights.ContainsKey(element))
                    _weights[element] = 0.0;
                _weights[element] += 1.0;
                _qList = _weights.Keys;
            }
            else
            {
                _qList = _qList.Concat(new[] { element });
            }

            if (States.Distinct().Count() > 1)
                SetType(QuantumStateType.SuperpositionAny);

            return this;
        }


        public static QuBit<T> WithEqualAmplitudes(IEnumerable<T> states)
        {
            var list = states.Distinct().ToList();
            double amp = 1.0 / Math.Sqrt(list.Count);
            var weighted = list.Select(s => (s, new Complex(amp, 0)));
            return new QuBit<T>(weighted);
        }
        #endregion

        #region Immutable Clone

        /// <summary>
        /// This quantum decision was final… but I copied it into a new universe where it wasn’t
        /// Cloning a collapsed QuBit resets its collapse status. The new instance retains the amplitudes and quantum state type, but is mutable and behaves as if never observed.
        /// </summary>
        public override QuantumSoup<T> Clone()
        {
            var clonedWeights = _weights != null ? new Dictionary<T, Complex>(_weights) : null;
            var clonedList = _qList.ToList();
            var clone = new QuBit<T>(clonedList, clonedWeights, _ops, _valueValidator);
            clone._isActuallyCollapsed = false;
            clone._isCollapsedFromSystem = false;
            clone._collapsedValue = default;
            clone._collapseHistoryId = null;
            clone._lastCollapseSeed = null;
            clone._eType = this._eType == QuantumStateType.CollapsedResult ? QuantumStateType.SuperpositionAny : this._eType;
            return clone;
        }


        #endregion

        #region Fields for Local Storage (used if _system == null)

        private IEnumerable<T> _qList;
        private static readonly IQuantumOperators<T> _defaultOps = QuantumOperatorsFactory.GetOperators<T>();


        public IQuantumOperators<T> Operators => _ops;

        #endregion
        #region State Type Helpers
        // Lets you toggle between 'All must be true', 'Any might be true', and 'Reality is now a lie'.
        // Think of it like mood settings, but for wavefunctions.
        // Also, it helps you avoid existential crises by keeping track of your quantum state.

        public QuantumStateType GetCurrentType() => _eType;



        public QuBit<T> Any() { SetType(QuantumStateType.SuperpositionAny); return this; }
        public QuBit<T> All() { SetType(QuantumStateType.SuperpositionAll); return this; }

        #endregion

        #region Arithmetic Helpers
        // Implements arithmetic on superpositions, because regular math wasn't confusing enough.
        // Bonus: Now you can multiply the feeling of indecision by a probability cloud.
        // Also, it avoids full key-pair expansion (M×N growth) by combining outputs.



        /// <summary>
        /// ObservationalArithmetic implements *observational” behavior.
        /// It collapses both operands (if needed) and then applies the operator
        /// on the observed (collapsed) values. The resulting qubit is created
        /// in a collapsed state.
        /// </summary>
        private QuBit<T> ObservationalArithmetic(QuBit<T> a, QuBit<T> b, Func<T, T, T> op)
        {
            // Collapse both qubits.
            // This will trigger the legacy collapse logic if they are not already collapsed.
            T observedA = a.Observe();
            T observedB = b.Observe();

            // Apply the operator to the collapsed (observed) values.
            T resultValue = op(observedA, observedB);

            // Create a new qubit that contains only the resulting value.
            // Assign a full weight (e.g. Complex.One) so that the new qubit
            // reflects a collapsed state.
            var collapsedStates = new List<T> { resultValue };
            var collapsedWeights = new Dictionary<T, Complex> { { resultValue, Complex.One } };
            var resultQubit = new QuBit<T>(collapsedStates, collapsedWeights, a.Operators);

            // Mark the new qubit as collapsed.
            resultQubit.SetType(QuantumStateType.CollapsedResult);
            resultQubit._isActuallyCollapsed = true;

            return resultQubit;
        }
        // In the QuBit<T> class, inside the region for arithmetic helpers:

        /// <summary>
        /// Performs the specified operation on two QuBit<T> instances.
        /// If non-observational arithmetic is enabled, combine the states
        /// without collapsing and cache results for pure commutative operations.
        /// Otherwise, collapse both operands and apply the operator on the observed values.
        /// </summary>
        private QuBit<T> Do_oper_type(QuBit<T> a, QuBit<T> b, Func<T, T, T> op)
        {
            if (QuantumConfig.EnableNonObservationalArithmetic)
            {
                // Combine the underlying state lists, using the provided operator.
                var newList = QuantumMathUtility<T>.CombineAll(a._qList, b._qList, op);
                Dictionary<T, Complex>? newWeights = null;

                // Create a local cache if commutative caching is enabled.
                Dictionary<CommutativeKey<T>, T>? cache = null;
                if (QuantumConfig.EnableCommutativeCache)
                {
                    cache = new Dictionary<CommutativeKey<T>, T>();
                }

                if (a._weights != null || b._weights != null)
                {
                    newWeights = new Dictionary<T, Complex>();
                    foreach (var (valA, wA) in a.ToWeightedValues())
                    {
                        foreach (var (valB, wB) in b.ToWeightedValues())
                        {
                            T newVal;
                            // Check cache for the unordered pair.
                            if (cache != null)
                            {
                                var key = new CommutativeKey<T>(valA, valB);
                                if (!cache.TryGetValue(key, out newVal))
                                {
                                    newVal = op(valA, valB);
                                    cache[key] = newVal;
                                }
                            }
                            else
                            {
                                newVal = op(valA, valB);
                            }

                            Complex combinedWeight = wA * wB;
                            if (newWeights.ContainsKey(newVal))
                                newWeights[newVal] += combinedWeight;
                            else
                                newWeights[newVal] = combinedWeight;
                        }
                    }
                }

                return new QuBit<T>(newList, newWeights, a._ops);
            }
            else
            {
                // Legacy (observational) branch: collapse both qubits then perform operator.
                return ObservationalArithmetic(a, b, op);
            }
        }

        /// <summary>
        /// Performs the specified operation on a QuBit<T> and a scalar (QuBit op scalar).
        /// If non-observational arithmetic is enabled, process each branch
        /// using caching for commutative operations; otherwise, collapse the qubit.
        /// </summary>
        private QuBit<T> Do_oper_type(QuBit<T> a, T b, Func<T, T, T> op)
        {
            if (!QuantumConfig.EnableNonObservationalArithmetic)
            {
                // Legacy: collapse the qubit first.
                T observedA = a.Observe();
                T resultValue = op(observedA, b);
                var collapsedStates = new List<T> { resultValue };
                var collapsedWeights = new Dictionary<T, Complex> { { resultValue, Complex.One } };
                var resultQubit = new QuBit<T>(collapsedStates, collapsedWeights, a.Operators);
                resultQubit.SetType(QuantumStateType.CollapsedResult);
                resultQubit._isActuallyCollapsed = true;
                return resultQubit;
            }

            var newList = QuantumMathUtility<T>.Combine(a._qList, b, op);
            Dictionary<T, Complex>? newWeights = null;

            // Set up a cache if commutative caching is enabled.
            Dictionary<CommutativeKey<T>, T>? cache = null;
            if (QuantumConfig.EnableCommutativeCache)
            {
                cache = new Dictionary<CommutativeKey<T>, T>();
            }

            if (a._weights != null)
            {
                newWeights = new Dictionary<T, Complex>();
                foreach (var (valA, wA) in a.ToWeightedValues())
                {
                    T newVal;
                    if (cache != null)
                    {
                        var key = new CommutativeKey<T>(valA, b);
                        if (!cache.TryGetValue(key, out newVal))
                        {
                            newVal = op(valA, b);
                            cache[key] = newVal;
                        }
                    }
                    else
                    {
                        newVal = op(valA, b);
                    }

                    if (!newWeights.ContainsKey(newVal))
                        newWeights[newVal] = Complex.Zero;
                    newWeights[newVal] += wA; // multiply by 1.0
                }
            }

            return new QuBit<T>(newList, newWeights, _ops);
        }

        /// <summary>
        /// Performs the specified operation on a scalar and a QuBit<T> (scalar op QuBit).
        /// If non-observational arithmetic is enabled, uses caching for commutative results;
        /// otherwise, collapses the qubit operand.
        /// </summary>
        private QuBit<T> Do_oper_type(T a, QuBit<T> b, Func<T, T, T> op)
        {
            if (!QuantumConfig.EnableNonObservationalArithmetic)
            {
                // Legacy: collapse the qubit before operating.
                T observedB = b.Observe();
                T resultValue = op(a, observedB);
                var collapsedStates = new List<T> { resultValue };
                var collapsedWeights = new Dictionary<T, Complex> { { resultValue, Complex.One } };
                var resultQubit = new QuBit<T>(collapsedStates, collapsedWeights, b.Operators);
                resultQubit.SetType(QuantumStateType.CollapsedResult);
                resultQubit._isActuallyCollapsed = true;
                return resultQubit;
            }

            var newList = QuantumMathUtility<T>.Combine(a, b._qList, op);
            Dictionary<T, Complex>? newWeights = null;

            Dictionary<CommutativeKey<T>, T>? cache = null;
            if (QuantumConfig.EnableCommutativeCache)
            {
                cache = new Dictionary<CommutativeKey<T>, T>();
            }

            if (b._weights != null)
            {
                newWeights = new Dictionary<T, Complex>();
                foreach (var (valB, wB) in b.ToWeightedValues())
                {
                    T newVal;
                    if (cache != null)
                    {
                        var key = new CommutativeKey<T>(a, valB);
                        if (!cache.TryGetValue(key, out newVal))
                        {
                            newVal = op(a, valB);
                            cache[key] = newVal;
                        }
                    }
                    else
                    {
                        newVal = op(a, valB);
                    }

                    if (!newWeights.ContainsKey(newVal))
                        newWeights[newVal] = Complex.Zero;
                    newWeights[newVal] += wB; // multiply by 1.0
                }
            }

            return new QuBit<T>(newList, newWeights, _ops);
        }

        #endregion

        #region Operator Overloads
        // Overloads basic math ops so you can add, subtract, multiply your way through parallel universes.
        // Because plain integers are too committed to a single outcome.


        public static QuBit<T> operator %(T a, QuBit<T> b) =>
            b.Do_oper_type(a, b, (x, y) => b._ops.Mod(x, y));
        public static QuBit<T> operator %(QuBit<T> a, QuBit<T> b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));
        public static QuBit<T> operator %(QuBit<T> a, T b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));

        public static QuBit<T> operator +(T a, QuBit<T> b) =>
            b.Do_oper_type(a, b, (x, y) => b._ops.Add(x, y));
        public static QuBit<T> operator +(QuBit<T> a, QuBit<T> b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));
        public static QuBit<T> operator +(QuBit<T> a, T b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));

        public static QuBit<T> operator -(T a, QuBit<T> b) =>
            b.Do_oper_type(a, b, (x, y) => b._ops.Subtract(x, y));
        public static QuBit<T> operator -(QuBit<T> a, QuBit<T> b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));
        public static QuBit<T> operator -(QuBit<T> a, T b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));

        public static QuBit<T> operator *(T a, QuBit<T> b) =>
            b.Do_oper_type(a, b, (x, y) => b._ops.Multiply(x, y));
        public static QuBit<T> operator *(QuBit<T> a, QuBit<T> b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));
        public static QuBit<T> operator *(QuBit<T> a, T b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));

        public static QuBit<T> operator /(T a, QuBit<T> b) =>
            b.Do_oper_type(a, b, (x, y) => b._ops.Divide(x, y));
        public static QuBit<T> operator /(QuBit<T> a, QuBit<T> b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));
        public static QuBit<T> operator /(QuBit<T> a, T b) =>
            a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));

        #endregion

        #region Introspection

        /// <summary>
        /// Observes (collapses) the QuBit with an optional Random instance for deterministic replay.
        /// If the QuBit is already collapsed, returns the previously collapsed value.
        /// If mock-collapse is enabled, returns the forced value without changing the underlying state.
        /// </summary>
        /// <param name="rng">Optional random instance for deterministic replay.</param>
        /// <returns>The collapsed (observed) value.</returns>
        public override T Observe(Random? rng = null)
        {
            return PerformCollapse(rng);
        }


        /// <summary>
        /// Performs the collapse logic:
        /// samples a value from the superposition, validates it,
        /// updates internal state (e.g. _collapsedValue, _qList, _weights),
        /// and marks the qubit as collapsed.
        /// </summary>
        public T PerformCollapse(Random? rng = null)
        {
            rng ??= Random.Shared;

            // If mock collapse is enabled, return the forced value.
            if (_mockCollapseEnabled)
            {
                if (_mockCollapseValue == null)
                    throw new InvalidOperationException("Mock collapse enabled but no mock value is set.");
                return _mockCollapseValue;
            }

            // If already collapsed, simply return the collapsed value.
            if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
            {
                return _collapsedValue;
            }

            // Like picking a random life choice and pretending you meant to do that all along.
            T picked = SampleWeighted(rng);

            if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
                throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");

            // Update internal state to reflect the collapse.
            _collapsedValue = picked;
            _qList = new[] { picked };
            if (_weights != null)
            {
                _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
            }
            SetType(QuantumStateType.CollapsedResult);
            _isActuallyCollapsed = true;

            return picked;
        }

        public void PartialCollapse(int[] chosenOutcome)
        {
            // Only update local state: set _collapsedValue based on _qubitIndices.
            if (_qubitIndices.Length == 1)
            {
                int value = chosenOutcome[0];
                // For bool qubits, convert 0/1 to false/true.
                object observed = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
                _collapsedValue = (T)observed;
            }
            else
            {
                // For multi-index qubits.
                int[] values = _qubitIndices.Select(i => chosenOutcome[i]).ToArray();
                _collapsedValue = (T)(object)values;
            }
            // Mark this qubit as collapsed locally.
            // (Do not update _amplitudes so that other qubits remain uncollapsed.)
            _isActuallyCollapsed = true;
            // Do NOT trigger propagation here.
        }



        /// <summary>
        /// Observes (collapses) the QuBit using a supplied integer seed for deterministic replay.
        /// </summary>
        public T Observe(int seed)
        {
            // store the last collapse seed for debugging
            _lastCollapseSeed = seed;
            _collapseHistoryId = Guid.NewGuid();

            var rng = new Random(seed);
            return Observe(rng);
        }

        public T ObserveInBasis(Complex[,] basisMatrix, Random? rng = null)
        {
            if (_weights == null || _weights.Count == 0)
                throw new InvalidOperationException("No amplitudes available for basis transform.");

            int dimension = _weights.Count;

            if (basisMatrix.GetLength(0) != dimension || basisMatrix.GetLength(1) != dimension)
                throw new ArgumentException($"Basis transform must be a {dimension}×{dimension} square matrix.");

            // Capture the states and their amplitudes
            var states = _weights.Keys.ToArray();
            var amplitudes = states.Select(s => _weights[s]).ToArray();

            // Apply the unitary basis transformation
            var transformed = QuantumMathUtility<Complex>.ApplyMatrix(amplitudes, basisMatrix);

            // Construct new weights
            var newWeights = new Dictionary<T, Complex>();
            for (int i = 0; i < states.Length; i++)
                newWeights[states[i]] = transformed[i];

            // Because nothing says “science” like measuring something after you’ve changed the rules.
            var newQubit = new QuBit<T>(states, newWeights, _ops).WithNormalisedWeights();
            return newQubit.Observe(rng);
        }



        /// <summary>
        /// Returns true if this QuBit has actually collapsed (via a real observation).
        /// </summary>
        public bool IsActuallyCollapsed => _isActuallyCollapsed && _eType == QuantumStateType.CollapsedResult;

        // ---------------------------------------------------------------------
        // Collapse Mocking
        // ---------------------------------------------------------------------

        /// <summary>
        /// Enables mock collapse, causing Observe() to always return forcedValue
        /// without modifying the underlying quantum state.
        /// </summary>
        public QuBit<T> WithMockCollapse(T forcedValue)
        {
            _mockCollapseEnabled = true;
            _mockCollapseValue = forcedValue;
            return this;
        }

        /// <summary>
        /// Disables mock collapse so that Observe() performs a real collapse.
        /// </summary>
        public QuBit<T> WithoutMockCollapse()
        {
            _mockCollapseEnabled = false;
            _mockCollapseValue = default;
            return this;
        }

        /// <summary>
        /// Returns a string representation of the current superposition states and their weights
        /// without triggering a collapse. Useful for debugging and introspection.
        /// </summary>
        public string show_states() => ToDebugString();

        #endregion

        #region Weighting and Output

        // Converts the tangled quantum mess into something printable,
        // so you can lie to yourself and pretend you understand what's going on.
        public IEnumerable<T> ToCollapsedValues()
        {
            if (!States.Any())
            {
                throw new InvalidOperationException(
                    $"No values to collapse. IsCollapsed={_isActuallyCollapsed}, StateCount={States.Count}, Type={_eType}."
                );
            }

            if (_eType == QuantumStateType.SuperpositionAny)
                return States;
            else if (_eType == QuantumStateType.SuperpositionAll)
            {
                var distinct = States.Distinct().ToList();
                return distinct.Count == 1 ? distinct : States;
            }
            else
            {
                // Collapsed
                return States.Take(1);
            }
        }

        /// <summary>
        /// Returns a collection of tuples containing the values and their corresponding weights.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(T value, Complex weight)> ToWeightedValues()
        {
            if (_weights == null)
            {
                // All distinct items with weight=1.0
                var distinct = _qList.Distinct();
                foreach (var v in distinct)
                    yield return (v, 1.0);
            }
            else
            {
                foreach (var kvp in _weights)
                    yield return (kvp.Key, kvp.Value);
            }
        }


        /// <summary>
        /// An explicit “WithNormalisedWeights()” 
        /// that returns a new QuBit or modifies in place. Here we clone for safety.
        /// </summary>
        public QuBit<T> WithNormalisedWeights()
        {
            if (!IsWeighted) return this; // no weights to normalise



            var clonedWeights = new Dictionary<T, Complex>(_weights);

            var clonedQList = _qList.ToList();  // or just _weights.Keys

            var newQ = new QuBit<T>(clonedQList, clonedWeights, _ops)

            {

                _eType = this._eType

            };

            newQ.NormaliseWeights();

            return newQ;

        }



        /// <summary>
        /// Optionally, modify WithWeights(...) to auto-normalise if desired:
        /// </summary>
        public QuBit<T> WithWeights(Dictionary<T, Complex> weights, bool autoNormalise = false)
        {
            if (weights == null)
                throw new ArgumentNullException(nameof(weights));

            // Filter weights to only include valid states.
            var filtered = new Dictionary<T, Complex>();
            foreach (var kvp in weights)
            {
                if (States.Contains(kvp.Key))
                    filtered[kvp.Key] = kvp.Value;
            }

            if (this.System != null)
            {
                // Update this instance’s weights in place.
                _weights = filtered;
                if (autoNormalise)
                    NormaliseWeights();
                return this;
            }
            else
            {
                // Because sometimes commitment is optional. Especially in quantum dating.
                var newQ = new QuBit<T>(_qList, filtered, _ops)
                {
                    _eType = this._eType
                };
                newQ._weights = filtered;
                if (autoNormalise)
                    newQ.NormaliseWeights();
                return newQ;
            }
        }


        /// <summary>
        /// Returns a string representation of the current superposition states.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Legacy style if no weights or all weights equal
            if (_weights == null || AllWeightsEqual(_weights))
            {
                var distinct = States.Distinct();
                if (_eType == QuantumStateType.SuperpositionAny)
                    return $"any({string.Join(", ", distinct)})";
                else
                {
                    if (distinct.Count() == 1) return distinct.First().ToString();
                    return $"all({string.Join(", ", distinct)})";
                }
            }
            else
            {
                // Weighted
                var entries = ToWeightedValues().Select(x => $"{x.value}:{x.weight}");
                return _eType == QuantumStateType.SuperpositionAny
                    ? $"any({string.Join(", ", entries)})"
                    : $"all({string.Join(", ", entries)})";
            }
        }

        /// <summary>
        /// Returns a string representation of the current superposition states and their weights
        /// </summary>
        /// <returns></returns>
        public string ToDebugString()
        {
            return string.Join(", ", ToWeightedValues()
                .Select(x => $"{x.value} (weight: {x.weight})"));
        }

        /// <summary>
        /// Returns a JSON string representation of the current superposition states and their weights.
        /// </summary>
        /// <returns></returns>
        public string ToJsonDebugString()
        {
            var obj = new
            {
                states = this.ToWeightedValues().Select(v => new {
                    value = v.value,
                    amplitude = new { real = v.weight.Real, imag = v.weight.Imaginary },
                    probability = v.weight.Magnitude * v.weight.Magnitude
                }),
                collapsed = this.IsCollapsed,
                collapseId = this.LastCollapseHistoryId,
                qubitIndices = this.GetQubitIndices()
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }


        /// <summary>
        /// Checks if all weights in the dictionary are equal.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private bool AllWeightsEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1) return true;
            var first = dict.Values.First();
            return dict.Values.Skip(1).All(w => Complex.Abs(w - first) < 1e-14);
        }

        /// <summary>
        /// Check Equal Probabilities (|amplitude|²)
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private bool AllWeightsProbablyEqual(Dictionary<T, Complex> dict)
        {
            if (dict.Count <= 1) return true;

            double firstProb = SquaredMagnitude(dict.Values.First());

            return dict.Values
                .Skip(1)
                .All(w => Math.Abs(SquaredMagnitude(w) - firstProb) < 1e-14);
        }

        private double SquaredMagnitude(Complex c) => c.Real * c.Real + c.Imaginary * c.Imaginary;

        #endregion

        /// <summary>
        /// Indicates if the QuBit is burdened with knowledge (i.e., weighted).
        /// If not, it's just blissfully unaware of how much it should care.
        /// </summary>
        public bool IsWeighted => _weights != null;

        public bool IsCollapsed => _isCollapsedFromSystem || _isActuallyCollapsed;

        public override IReadOnlyCollection<T> States =>
        _weights != null ? (IReadOnlyCollection<T>)_weights.Keys : _qList.ToList();

        /// <summary>
        /// Returns the most probable state, i.e., the one that's been yelling the loudest in the multiverse.
        /// This is as close to democracy as quantum physics gets.
        /// </summary>
        public T MostProbable()
        {
            if (!States.Any())
                throw new InvalidOperationException("No states available to collapse.");

            if (!IsWeighted)
            {
                // fallback
                return ToCollapsedValues().First();
            }
            else
            {
                // pick the max by weight
                var (val, _) = ToWeightedValues()
                    .OrderByDescending(x => x.weight)
                    .First();
                return val;
            }
        }

        /// <summary>
        /// Tolerance used for comparing weights in equality checks.
        /// This allows for minor floating-point drift.
        /// </summary>
        private static readonly double _tolerance = 1e-9;


        /// <summary>
        /// Creates a hash that reflects the spiritual essence of your quantum mess.
        /// If you're lucky, two equal QuBits won't hash to the same black hole
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                foreach (var s in States.Distinct().OrderBy(x => x))
                {
                    hash = hash * 23 + (s?.GetHashCode() ?? 0);

                    if (IsWeighted)
                    {
                        Complex amp = _weights != null && _weights.TryGetValue(s, out var w) ? w : Complex.One;

                        // Round real and imaginary parts separately to avoid hash instability
                        double real = Math.Round(amp.Real, 12);
                        double imag = Math.Round(amp.Imaginary, 12);

                        long realBits = BitConverter.DoubleToInt64Bits(real);
                        long imagBits = BitConverter.DoubleToInt64Bits(imag);

                        hash = hash * 23 + realBits.GetHashCode();
                        hash = hash * 23 + imagBits.GetHashCode();
                    }
                }

                return hash;
            }
        }


        /// <summary>
        /// Provides a tiny status report that says: "Yes, you're still lost in the matrix."
        /// </summary>
        public string WeightSummary()
        {
            if (!IsWeighted) return "Weighted: false";

            double totalProbability = _weights.Values.Sum(amp => amp.Magnitude * amp.Magnitude);
            double maxMagnitudeSquared = _weights.Values.Max(amp => amp.Magnitude * amp.Magnitude);
            double minMagnitudeSquared = _weights.Values.Min(amp => amp.Magnitude * amp.Magnitude);

            return $"Weighted: true, Sum(|amp|²): {totalProbability}, Max(|amp|²): {maxMagnitudeSquared}, Min(|amp|²): {minMagnitudeSquared}";
        }


        /// <summary>
        /// Implicitly collapses the QuBit and returns a single value.
        /// <para>
        /// This is provided strictly for poetic license, lazy prototyping,
        /// and chaotic neutral debugging.
        /// </para>
        /// <para>
        /// This performs a probabilistic collapse (SampleWeighted) without preserving collapse state.
        /// </para>
        /// </summary>
        /// <param name="q"></param>
        [Obsolete("Implicit QuBit collapse is not safe for production. Use Observe() instead.")]
        public static implicit operator T(QuBit<T> q) => q.SampleWeighted();

        // A convenient way to slap some probabilities onto your states after the fact.
        // Like putting sprinkles on a Schrödinger cupcake — you won't know how it tastes until you eat it, and then it's too late.
        /// </summary>
        public QuBit<T> WithWeightsNormalised(Dictionary<T, Complex> weights)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));

            // Gather only the weights for keys we already have in States.
            var filtered = new Dictionary<T, Complex>();
            foreach (var kvp in weights)
            {
                if (States.Contains(kvp.Key))
                    filtered[kvp.Key] = kvp.Value;
            }

            // Construct the new QuBit with the same _qList, same ops
            var newQ = new QuBit<T>(_qList, filtered, _ops)
            {
                _eType = this._eType  // preserve the existing quantum state type
            };
            return newQ;
        }

        public int[] GetQubitIndices() => _qubitIndices;

        public void NotifyWavefunctionCollapsed(Guid collapseId)
        {
            // If already collapsed, ensure we update the system-observed value if missing.
            if (_isActuallyCollapsed)
            {
                if (_systemObservedValue == null && _system != null)
                {
                    int[] fullObserved;
                    try
                    {
                        fullObserved = _system.GetCollapsedState();
                    }
                    catch (InvalidOperationException)
                    {
                        int[] unionIndices = GetUnionOfGroupIndices();
                        fullObserved = _system.ObserveGlobal(unionIndices, Random.Shared);
                    }

                    if (_qubitIndices.Length == 1)
                    {
                        var value = fullObserved[_qubitIndices[0]];
                        object val = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
                        _systemObservedValue = val;
                        _collapsedValue = (T)val;
                    }
                    else
                    {
                        var values = _qubitIndices.Select(i => fullObserved[i]).ToArray();
                        object val = typeof(T) == typeof(bool[]) ? (object)values.Select(v => v != 0).ToArray() : (object)values;
                        _systemObservedValue = val;
                        if (val is T t)
                            _collapsedValue = t;
                    }
                }
                _collapseHistoryId = collapseId;
                return;
            }

            _isCollapsedFromSystem = true;
            _collapseHistoryId = collapseId;

            if (_system != null)
            {
                int[] fullObserved;
                try
                {
                    fullObserved = _system.GetCollapsedState();
                }
                catch (InvalidOperationException)
                {
                    int[] unionIndices = GetUnionOfGroupIndices();
                    fullObserved = _system.ObserveGlobal(unionIndices, Random.Shared);
                }

                if (_qubitIndices.Length == 1)
                {
                    var value = fullObserved[_qubitIndices[0]];
                    object val = typeof(T) == typeof(bool) ? (object)(value != 0) : (object)value;
                    _systemObservedValue = val;
                    _collapsedValue = (T)val;
                }
                else
                {
                    var values = _qubitIndices.Select(i => fullObserved[i]).ToArray();
                    object val = typeof(T) == typeof(bool[]) ? (object)values.Select(v => v != 0).ToArray() : (object)values;
                    _systemObservedValue = val;
                    if (val is T t)
                        _collapsedValue = t;
                }

                _isActuallyCollapsed = true;
                _eType = QuantumStateType.CollapsedResult;
            }
        }



        public object? GetObservedValue()
        {
            if (IsCollapsed)
            {
                // If we collapsed locally, return _collapsedValue
                if (_collapsedValue != null) return _collapsedValue;

                // If the system forced the collapse, we might do a lazy read:
                // For a real partial measurement approach, you'd query
                // the system's wavefunction. Here we do a simplified approach
                // if T == (int,bool).
                if (_systemObservedValue != null) return _systemObservedValue;
            }

            return null;
        }



        object IQuantumReference.Observe(Random? rng)
        {
            if (_system != null)
            {
                if (IsCollapsed)
                {
                    if (_systemObservedValue != null) return _systemObservedValue;
                    if (_collapsedValue != null) return _collapsedValue;
                }
                rng ??= Random.Shared;
                // Collapse the full entangled state by measuring the union of indices
                int[] unionIndices = GetUnionOfGroupIndices();
                int[] measured = _system.ObserveGlobal(unionIndices, rng);

                // Now extract this qubit’s value from the complete collapsed state.
                object result;
                if (_qubitIndices.Length == 1)
                {
                    result = measured[_qubitIndices[0]];
                }
                else
                {
                    int[] myValues = _qubitIndices.Select(i => measured[i]).ToArray();
                    result = myValues;
                }
                _systemObservedValue = result;
                return result;
            }

            // Fall back to local collapse if no system is associated.
            return ObserveLocal(rng);
        }


        public void ApplyLocalUnitary(Complex[,] gate, string gateName)
        {
            if (this.System != null)
            {
                // For example, if this is a single-qubit operation:
                if (_qubitIndices.Length == 1 && gate.GetLength(0) == 2 && gate.GetLength(1) == 2)
                {
                    this.System.ApplySingleQubitGate(_qubitIndices[0], gate, gateName);
                }
                else if (_qubitIndices.Length == 2 && gate.GetLength(0) == 4 && gate.GetLength(1) == 4)
                {
                    this.System.ApplyTwoQubitGate(_qubitIndices[0], _qubitIndices[1], gate, gateName);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported gate size or qubit index count.");
                }
                return;
            }

            // Local fallback for 2-state qubits
            if (_weights == null || _weights.Count != 2)
                throw new InvalidOperationException("Local unitary only supported for 2-state local qubits in this example.");

            var states = _weights.Keys.ToArray();
            var amplitudes = states.Select(s => _weights[s]).ToArray();

            var transformed = QuantumMathUtility<Complex>.ApplyMatrix(amplitudes, gate);

            for (int i = 0; i < states.Length; i++)
            {
                _weights[states[i]] = transformed[i];
            }

            NormaliseWeights();
        }


        #region Local Collapse Logic

        /// <summary>
        /// The original local collapse logic.
        /// </summary>
        private object ObserveLocal(Random? rng)
        {
            // If mock collapse is enabled, return the forced mock value
            if (_mockCollapseEnabled)
            {
                if (_mockCollapseValue == null)
                    throw new InvalidOperationException(
                        $"Mock collapse enabled but no mock value is set. IsCollapsed={_isActuallyCollapsed}, States.Count={States.Count}, Type={_eType}."
                    );
                return _mockCollapseValue;
            }

            // If already collapsed, return the same value
            if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
            {
                return _collapsedValue;
            }

            rng ??= Random.Shared;

            T picked = SampleWeighted(rng);

            // Use the configuration flag and value validator to protect against default(T)
            if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
            {
                throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
            }

            if (EqualityComparer<T>.Default.Equals(picked, default(T)))
            {
                throw new InvalidOperationException(
                    $"Collapse resulted in default value. IsCollapsed={_isActuallyCollapsed}, States.Count={States.Count}, Type={_eType}."
                );
            }

            // Mark as collapsed
            _collapsedValue = picked;
            _isActuallyCollapsed = true;
            _qList = new[] { picked };
            if (_weights != null)
            {
                _weights = new Dictionary<T, Complex> { { picked, Complex.One } };
            }
            SetType(QuantumStateType.CollapsedResult);

            return picked!;
        }

        #endregion

    }
}
