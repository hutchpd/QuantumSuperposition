// Because quantum code should be at least as confusing as quantum physics.
#region QuantumCore

/// <summary>
/// Represents the current mood of the QuBit. Is it feeling inclusive (All)?
/// Or indecisive (Any)? Or has it finally made up its mind (Collapsed)?
/// </summary>
public enum QuantumStateType
{
    Conjunctive,      // All states must be true. Like group projects, but successful.
    Disjunctive,      // Any state can be true. Like excuses for missing deadlines.
    CollapsedResult   // Only one state remains after collapse. R.I.P. potential.
}

// A set of mathematical operations tailored for types that wish they were numbers.
public interface IQuantumOperators<T>
{
    T Add(T a, T b);
    T Subtract(T a, T b);
    T Multiply(T a, T b);
    T Divide(T a, T b);
    T Mod(T a, T b);
    bool GreaterThan(T a, T b);
    bool GreaterThanOrEqual(T a, T b);
    bool LessThan(T a, T b);
    bool LessThanOrEqual(T a, T b);
    bool Equal(T a, T b);
    bool NotEqual(T a, T b);
}

// Like a gym trainer, but for ints. Does the usual heavy lifting.
public class IntOperators : IQuantumOperators<int>
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public int Divide(int a, int b) => a / b;
    public int Mod(int a, int b) => a % b;
    public bool GreaterThan(int a, int b) => a > b;
    public bool GreaterThanOrEqual(int a, int b) => a >= b;
    public bool LessThan(int a, int b) => a < b;
    public bool LessThanOrEqual(int a, int b) => a <= b;
    public bool Equal(int a, int b) => a == b;
    public bool NotEqual(int a, int b) => a != b;
}

// Chooses the appropriate operator set for a given type.
// Currently supports int. Future support may include imaginary friends.
public static class QuantumOperatorsFactory
{
    public static IQuantumOperators<T> GetOperators<T>()
    {
        if (typeof(T) == typeof(int))
            return (IQuantumOperators<T>)(object)new IntOperators();
        throw new NotImplementedException("Default operators not implemented for type " + typeof(T));
    }
}

// Utility for combining values. Think of it like a quantum kitchen mixer.
public static class QuantumMathUtility<T>
{
    public static IEnumerable<T> CombineAll(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, T> op) =>
        a.SelectMany(x => b.Select(y => op(x, y)));

    public static IEnumerable<T> Combine(IEnumerable<T> a, T b, Func<T, T, T> op) =>
        a.Select(x => op(x, b));

    public static IEnumerable<T> Combine(T a, IEnumerable<T> b, Func<T, T, T> op) =>
        b.Select(x => op(a, x));
}

#endregion

#region QuBit<T> Implementation

/// <summary>
/// QuBit<T> represents a superposition of values of type T, optionally weighted.
/// It's like Schrödinger's inbox: everything's unread and somehow also read.
/// </summary>
public partial class QuBit<T>
{
    private IEnumerable<T> _qList;
    private Dictionary<T, double>? _weights; // optional weighting
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops;
    private static readonly IQuantumOperators<T> _defaultOps = QuantumOperatorsFactory.GetOperators<T>();

    public IReadOnlyCollection<T> States =>
        _weights != null ? (IReadOnlyCollection<T>)_weights.Keys : _qList.ToList();

    public IQuantumOperators<T> Operators => _ops;

    #region Constructors
    // Constructors that enable you to manifest chaotic energy into a typed container.
    // Also known as: Creating a mess in a mathematically defensible way.
    public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qList = Items;

        // If multiple distinct items, set to Disjunctive
        if (_qList.Distinct().Count() > 1)
            SetType(QuantumStateType.Disjunctive);
    }

    public QuBit(IEnumerable<T> Items) : this(Items, _defaultOps) { }

    public QuBit(IEnumerable<(T value, double weight)> weightedItems, IQuantumOperators<T> ops)
    {
        if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        var dict = new Dictionary<T, double>();
        foreach (var (val, w) in weightedItems)
        {
            if (!dict.ContainsKey(val))
                dict[val] = 0.0;
            dict[val] += w;
        }
        _weights = dict;
        _qList = dict.Keys; // keep a fallback list of keys

        if (_weights.Count > 1)
            SetType(QuantumStateType.Disjunctive);
    }

    public QuBit(IEnumerable<(T value, double weight)> weightedItems)
        : this(weightedItems, _defaultOps)
    {
    }

    internal QuBit(IEnumerable<T> items, Dictionary<T, double>? weights, IQuantumOperators<T> ops)
    {
        _qList = items;
        _weights = weights;
        _ops = ops;

        if (States.Distinct().Count() > 1)
            SetType(QuantumStateType.Disjunctive);
    }

    #endregion

    #region State Type Helpers
    // Lets you toggle between 'All must be true', 'Any might be true', and 'Reality is now a lie'.
    // Think of it like mood settings, but for wavefunctions.
    // Also, it helps you avoid existential crises by keeping track of your quantum state.

    public QuantumStateType GetCurrentType() => _eType;
    private void SetType(QuantumStateType t) => _eType = t;

    public QuBit<T> Append(T element)
    {
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
            SetType(QuantumStateType.Disjunctive);

        return this;
    }

    public QuBit<T> Any() { SetType(QuantumStateType.Disjunctive); return this; }
    public QuBit<T> All() { SetType(QuantumStateType.Conjunctive); return this; }

    #endregion

    #region Arithmetic Helpers
    // Implements arithmetic on superpositions, because regular math wasn't confusing enough.
    // Bonus: Now you can multiply the feeling of indecision by a probability cloud.
    // Also, it avoids full key-pair expansion (M×N growth) by combining outputs.

    /// <summary>
    /// Performs the specified operation on two QuBit<T> instances or a QuBit<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(QuBit<T> a, QuBit<T> b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.CombineAll(a._qList, b._qList, op);

        Dictionary<T, double>? newWeights = null;
        if (a._weights != null || b._weights != null)
        {
            newWeights = new Dictionary<T, double>();
            foreach (var (valA, wA) in a.ToWeightedValues())
            {
                foreach (var (valB, wB) in b.ToWeightedValues())
                {
                    var newVal = op(valA, valB);
                    var combinedWeight = wA * wB;
                    if (!newWeights.ContainsKey(newVal))
                        newWeights[newVal] = 0.0;
                    newWeights[newVal] += combinedWeight;
                }
            }
        }

        return new QuBit<T>(newList, newWeights, _ops);
    }

    /// <summary>
    /// Performs the specified operation on a QuBit<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(QuBit<T> a, T b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.Combine(a._qList, b, op);

        Dictionary<T, double>? newWeights = null;
        if (a._weights != null)
        {
            newWeights = new Dictionary<T, double>();
            foreach (var (valA, wA) in a.ToWeightedValues())
            {
                var newVal = op(valA, b);
                if (!newWeights.ContainsKey(newVal))
                    newWeights[newVal] = 0.0;
                newWeights[newVal] += wA; // multiply by 1.0
            }
        }

        return new QuBit<T>(newList, newWeights, _ops);
    }

    /// <summary>
    /// Performs the specified operation on a scalar and a QuBit<T>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private QuBit<T> Do_oper_type(T a, QuBit<T> b, Func<T, T, T> op)
    {
        var newList = QuantumMathUtility<T>.Combine(a, b._qList, op);

        Dictionary<T, double>? newWeights = null;
        if (b._weights != null)
        {
            newWeights = new Dictionary<T, double>();
            foreach (var (valB, wB) in b.ToWeightedValues())
            {
                var newVal = op(a, valB);
                if (!newWeights.ContainsKey(newVal))
                    newWeights[newVal] = 0.0;
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

    #region Evaluation / Collapse
    // Reality check: converts quantum indecision into a final verdict.
    // Useful when you want your data to stop being philosophical.

    public bool EvaluateAll()
    {
        SetType(QuantumStateType.Conjunctive);
        return States.All(state => !EqualityComparer<T>.Default.Equals(state, default(T)));
    }

    #endregion

    #region Weighting and Output

    // Converts the tangled quantum mess into something you can actually print,
    // while making you *feel* like you've accomplished something deterministic.

    public IEnumerable<T> ToValues()
    {
        if (!States.Any())
            throw new InvalidOperationException("No values to collapse.");

        if (_eType == QuantumStateType.Disjunctive)
            return States;
        else if (_eType == QuantumStateType.Conjunctive)
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

    public IEnumerable<(T value, double weight)> ToWeightedValues()
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

    public void NormalizeWeights()
    {
        if (_weights == null) return;
        double sum = _weights.Values.Sum();
        if (Math.Abs(sum) < double.Epsilon) return; // avoid dividing by zero

        var keys = _weights.Keys.ToList();
        foreach (var k in keys)
            _weights[k] /= sum;
    }

    private bool AllWeightsEqual(Dictionary<T, double> dict)
    {
        if (dict.Count <= 1) return true;
        var first = dict.Values.First();
        return dict.Values.Skip(1).All(w => Math.Abs(w - first) < 1e-14);
    }

    public override string ToString()
    {
        // Legacy style if no weights or all weights equal
        if (_weights == null || AllWeightsEqual(_weights))
        {
            var distinct = States.Distinct();
            if (_eType == QuantumStateType.Disjunctive)
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
            return _eType == QuantumStateType.Disjunctive
                ? $"any({string.Join(", ", entries)})"
                : $"all({string.Join(", ", entries)})";
        }
    }

    public string ToDebugString()
    {
        return string.Join(", ", ToWeightedValues()
            .Select(x => $"{x.value} (weight: {x.weight})"));
    }

    #endregion

    /// <summary>
    /// Indicates if the QuBit is burdened with knowledge (i.e., weighted).
    /// If not, it's just blissfully unaware of how much it should care.
    /// </summary>
    public bool IsWeighted => _weights != null;

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
            return ToValues().First();
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
    /// Does a little dance, rolls a quantum die, picks a value. May cause minor existential dread.
    /// </summary>
    public T SampleWeighted(Random? rng = null)
    {
        if (!States.Any())
            throw new InvalidOperationException("No states available to sample.");

        rng ??= Random.Shared;

        if (!IsWeighted)
        {
            // fallback
            return ToValues().First();
        }

        double total = _weights!.Values.Sum();
        if (total <= 1e-15)
        {
            // If all zero, fallback
            return ToValues().First();
        }

        double roll = rng.NextDouble() * total;
        double cumulative = 0.0;
        foreach (var (key, weight) in _weights)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return key;
        }

        // Fallback safety, though normally we should have returned inside the loop
        return _weights.Last().Key;
    }

    private static readonly double _tolerance = 1e-12;

    /// <summary>
    /// Compares two QuBits with a hypersensitive balance scale and emotional baggage tolerance.
    /// </summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not QuBit<T> other) return false;

        // Compare distinct sets of states
        var mySet = States.Distinct().ToHashSet();
        var otherSet = other.States.Distinct().ToHashSet();
        if (!mySet.SetEquals(otherSet)) return false;

        // If neither is weighted, they are equal as long as states match
        if (!this.IsWeighted && !other.IsWeighted)
        {
            return true;
        }

        // If either is weighted, compare all matching weights within tolerance
        foreach (var s in mySet)
        {
            double w1 = 1.0, w2 = 1.0;
            if (_weights != null && _weights.TryGetValue(s, out var val1)) w1 = val1;
            if (other._weights != null && other._weights.TryGetValue(s, out var val2)) w2 = val2;
            if (Math.Abs(w1 - w2) > _tolerance) return false;
        }
        return true;
    }

    /// <summary>
    /// Creates a hash that reflects the spiritual essence of your quantum mess.
    /// If you're lucky, two equal QuBits won't hash to the same black hole
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            // Combine the hash of all distinct states plus their weights if weighted
            int hash = 17;
            foreach (var s in States.Distinct().OrderBy(x => x))
            {
                hash = hash * 23 + s?.GetHashCode() ?? 0;
                if (IsWeighted)
                {
                    double w = _weights != null && _weights.TryGetValue(s, out var ww) ? ww : 1.0;
                    // Convert weight to a “rounded” form so that small changes won't produce different hashes
                    long bits = BitConverter.DoubleToInt64Bits(Math.Round(w, 12));
                    hash = hash * 23 + bits.GetHashCode();
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
        double sum = _weights!.Values.Sum();
        double max = _weights.Values.Max();
        double min = _weights.Values.Min();
        return $"Weighted: true, Sum: {sum}, Max: {max}, Min: {min}";
    }

    /// <summary>
    /// Implicit operator lets you pretend a QuBit is just a normal value.
    /// It isn’t. But sure. Keep pretending.
    /// </summary>
    /// <param name="q"></param>
    public static implicit operator T(QuBit<T> q) => q.SampleWeighted();

    // A convenient way to slap some probabilities onto your states after the fact.
    // Like putting sprinkles on top of a Schrödinger cupcake.
    /// </summary>
    public QuBit<T> WithWeights(Dictionary<T, double> weights)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));

        // Gather only the weights for keys we already have in States.
        var filtered = new Dictionary<T, double>();
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
}

#endregion

#region Eigenstates<T> Implementation

/// <summary>
/// Eigenstates<T> preserves original input keys in a dictionary Key->Value,
/// because sometimes you just want your quantum states to stop changing the subject.
/// </summary>
public class Eigenstates<T>
{
    private Dictionary<T, T> _qDict;
    private Dictionary<T, double>? _weights; // optional weighting
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops;

    public IReadOnlyCollection<T> States => _qDict.Keys;

    #region Constructors
    // Constructors that let you map values to themselves or to other values.
    // Also doubles as a safe space for anyone scared of full superposition commitment.

    public Eigenstates(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qDict = Items.ToDictionary(x => x, x => x);
    }

    public Eigenstates(IEnumerable<T> Items)
        : this(Items, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection, IQuantumOperators<T> ops)
    {
        if (inputValues == null) throw new ArgumentNullException(nameof(inputValues));
        if (projection == null) throw new ArgumentNullException(nameof(projection));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        _qDict = inputValues.ToDictionary(x => x, projection);
    }

    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection)
        : this(inputValues, projection, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    public Eigenstates(IEnumerable<(T value, double weight)> weightedItems, IQuantumOperators<T> ops)
    {
        if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        var dict = new Dictionary<T, double>();
        foreach (var (val, w) in weightedItems)
        {
            if (!dict.ContainsKey(val))
                dict[val] = 0.0;
            dict[val] += w;
        }

        _qDict = dict.Keys.ToDictionary(x => x, x => x);
        _weights = dict;
    }

    public Eigenstates(IEnumerable<(T value, double weight)> weightedItems)
        : this(weightedItems, QuantumOperatorsFactory.GetOperators<T>())
    {
    }

    internal Eigenstates(Dictionary<T, T> dict, IQuantumOperators<T> ops)
    {
        _ops = ops;
        _qDict = dict;
    }

    #endregion

    #region Filtering Mode
    // Toggle between “any of these are fine” and “they all better agree”.
    // Like relationship statuses but for probability distributions.

    public Eigenstates<T> Any() { _eType = QuantumStateType.Disjunctive; return this; }
    public Eigenstates<T> All() { _eType = QuantumStateType.Conjunctive; return this; }

    #endregion

    #region Arithmetic Operations

    // When you want to do math to your eigenstates and still feel like a functional adult.
    // Avoids full combinatorial meltdown, unlike your weekend.
    // Also, it’s like a quantum blender: it mixes everything together but still keeps the labels.
    // Because who needs clarity when you can have chaos?

    /// <summary>
    /// Performs the specified operation on two Eigenstates<T> instances or an Eigenstates<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, T> op)
    {
        // Use a dictionary to accumulate combined weights keyed by the computed result value.
        var newWeights = new Dictionary<T, double>();

        // Loop through all weighted values from both operands.
        foreach (var (valA, wA) in a.ToWeightedValues())
        {
            foreach (var (valB, wB) in b.ToWeightedValues())
            {
                var newValue = op(valA, valB);
                double combinedWeight = wA * wB;
                newWeights[newValue] = newWeights.TryGetValue(newValue, out var existing)
                    ? existing + combinedWeight
                    : combinedWeight;
            }
        }

        // Create a new key->value mapping where each key maps to itself.
        var newDict = newWeights.Keys.ToDictionary(x => x, x => x);
        var e = new Eigenstates<T>(newDict, a._ops)
        {
            _weights = newWeights
        };
        return e;
    }

    /// <summary>
    /// Performs the specified operation on an Eigenstates<T> and a scalar.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, T b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, double>? newWeights = null;

        if (a._weights != null)
            newWeights = new Dictionary<T, double>();

        foreach (var kvp in a._qDict)
        {
            result[kvp.Key] = op(kvp.Value, b);

            if (newWeights != null)
            {
                double wA = a._weights != null && a._weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                newWeights[kvp.Key] = wA;
            }
        }

        var e = new Eigenstates<T>(result, a._ops);
        e._weights = newWeights;
        return e;
    }

    /// <summary>
    /// Performs the specified operation on a scalar and an Eigenstates<T>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    private Eigenstates<T> Do_oper_type(T a, Eigenstates<T> b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, double>? newWeights = null;

        if (b._weights != null)
            newWeights = new Dictionary<T, double>();

        foreach (var kvp in b._qDict)
        {
            result[kvp.Key] = op(a, kvp.Value);

            if (newWeights != null)
            {
                double wB = b._weights != null && b._weights.TryGetValue(kvp.Key, out var bw) ? bw : 1.0;
                newWeights[kvp.Key] = wB;
            }
        }

        var e = new Eigenstates<T>(result, b._ops);
        e._weights = newWeights;
        return e;
    }

    public static Eigenstates<T> operator %(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Mod(x, y));
    public static Eigenstates<T> operator %(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));
    public static Eigenstates<T> operator %(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Mod(x, y));

    public static Eigenstates<T> operator +(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Add(x, y));
    public static Eigenstates<T> operator +(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));
    public static Eigenstates<T> operator +(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Add(x, y));

    public static Eigenstates<T> operator -(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Subtract(x, y));
    public static Eigenstates<T> operator -(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));
    public static Eigenstates<T> operator -(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Subtract(x, y));

    public static Eigenstates<T> operator *(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Multiply(x, y));
    public static Eigenstates<T> operator *(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));
    public static Eigenstates<T> operator *(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Multiply(x, y));

    public static Eigenstates<T> operator /(T a, Eigenstates<T> b) =>
        b.Do_oper_type(a, b, (x, y) => b._ops.Divide(x, y));
    public static Eigenstates<T> operator /(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));
    public static Eigenstates<T> operator /(Eigenstates<T> a, T b) =>
        a.Do_oper_type(a, b, (x, y) => a._ops.Divide(x, y));

    #endregion

    #region Filtering (Comparison) Operators
    // Let’s you ask “which of you is actually greater than 5?” in a very judgmental way.
    // Returns a trimmed-down existential crisis with weights.

    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, T value)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, double>? newWeights = null;

        if (_weights != null)
            newWeights = new Dictionary<T, double>();

        foreach (var kvp in _qDict)
        {
            if (condition(kvp.Value, value))
            {
                result[kvp.Key] = kvp.Value;
                if (newWeights != null)
                {
                    double wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                    newWeights[kvp.Key] = wA;
                }
            }
        }
        var e = new Eigenstates<T>(result, _ops);
        e._weights = newWeights;
        return e;
    }

    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, Eigenstates<T> other)
    {
        var result = new Dictionary<T, T>();
        Dictionary<T, double>? newWeights = null;

        if (_weights != null || other._weights != null)
            newWeights = new Dictionary<T, double>();

        foreach (var kvp in _qDict)
        {
            foreach (var kvp2 in other._qDict)
            {
                if (condition(kvp.Value, kvp2.Value))
                {
                    result[kvp.Key] = kvp.Value;
                    if (newWeights != null)
                    {
                        double wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                        double wB = other._weights != null && other._weights.TryGetValue(kvp2.Key, out var bw) ? bw : 1.0;
                        newWeights[kvp.Key] = wA * wB;
                    }
                    break; // break on first match
                }
            }
        }

        var e = new Eigenstates<T>(result, _ops);
        e._weights = newWeights;
        return e;
    }

    public static Eigenstates<T> operator <=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.LessThanOrEqual(x, y), b);
    public static Eigenstates<T> operator <=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.LessThanOrEqual(x, y), b);
    public static Eigenstates<T> operator >=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThanOrEqual(x, y), b);
    public static Eigenstates<T> operator >=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThanOrEqual(x, y), b);
    public static Eigenstates<T> operator <(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.LessThan(x, y), b);
    public static Eigenstates<T> operator <(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.LessThan(x, y), b);
    public static Eigenstates<T> operator >(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThan(x, y), b);
    public static Eigenstates<T> operator >(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.GreaterThan(x, y), b);
    public static Eigenstates<T> operator ==(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.Equal(x, y), b);
    public static Eigenstates<T> operator ==(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.Equal(x, y), b);
    public static Eigenstates<T> operator !=(Eigenstates<T> a, T b) =>
        a.Do_condition_type((x, y) => a._ops.NotEqual(x, y), b);
    public static Eigenstates<T> operator !=(Eigenstates<T> a, Eigenstates<T> b) =>
        a.Do_condition_type((x, y) => a._ops.NotEqual(x, y), b);

    #endregion

    #region Weights / Output
    // The part where we pretend our quantum data is readable by humans.
    // Also provides debugging strings to impress people on code reviews.

    public IEnumerable<T> ToValues() => _qDict.Keys;

    public override string ToString()
    {
        if (_weights == null || AllWeightsEqual(_weights))
        {
            return _eType == QuantumStateType.Disjunctive
                ? $"any({string.Join(", ", _qDict.Keys.Distinct())})"
                : $"all({string.Join(", ", _qDict.Keys.Distinct())})";
        }
        else
        {
            var pairs = ToWeightedValues().Select(x => $"{x.value}:{x.weight}");
            return _eType == QuantumStateType.Disjunctive
                ? $"any({string.Join(", ", pairs)})"
                : $"all({string.Join(", ", pairs)})";
        }
    }

    public IEnumerable<(T value, double weight)> ToWeightedValues()
    {
        if (_weights == null)
        {
            foreach (var k in _qDict.Keys.Distinct())
                yield return (k, 1.0);
        }
        else
        {
            foreach (var kvp in _weights)
                yield return (kvp.Key, kvp.Value);
        }
    }

    public void NormalizeWeights()
    {
        if (_weights == null) return;
        double sum = _weights.Values.Sum();
        if (Math.Abs(sum) < double.Epsilon) return;

        var keys = _weights.Keys.ToList();
        foreach (var k in keys)
            _weights[k] /= sum;
    }

    private bool AllWeightsEqual(Dictionary<T, double> dict)
    {
        if (dict.Count <= 1) return true;
        double first = dict.Values.First();
        return dict.Values.Skip(1).All(x => Math.Abs(x - first) < 1e-14);
    }

    public string ToDebugString()
    {
        if (_weights == null)
        {
            return string.Join(", ",
                _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"));
        }
        else
        {
            return string.Join(", ",
                _qDict.Select(kvp =>
                {
                    double w = _weights.TryGetValue(kvp.Key, out var val) ? val : 1.0;
                    return $"{kvp.Key} => {kvp.Value} (weight: {w})";
                }));
        }
    }

    #endregion

    /// <summary>
    /// Indicates whether your states have developed opinions (i.e., weights).
    /// If false, they're blissfully indifferent.
    /// </summary>
    public bool IsWeighted => _weights != null;

    /// <summary>
    /// Grabs the top N keys based on weight. 
    /// Sort of like picking favorites, but mathematically justified.
    /// </summary>
    public IEnumerable<T> TopNByWeight(int n)
    {
        if (!States.Any() || n <= 0)
            return Enumerable.Empty<T>();

        // If unweighted, each key is weight=1 => they are all "tied".
        // We'll just return them in natural order, limited to N.
        if (!IsWeighted)
            return States.Take(n);

        // Weighted => sort descending by weight
        return _weights!
            .OrderByDescending(kvp => kvp.Value)
            .Take(n)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Filter your states by weight like you're trimming down party invites.
    /// Only show up if your weight is greater than 0.75, Chad.”
    /// </summary>
    public Eigenstates<T> FilterByWeight(Func<double, bool> predicate)
    {
        var newDict = new Dictionary<T, T>();
        Dictionary<T, double>? newWeights = null;

        if (IsWeighted)
        {
            newWeights = new Dictionary<T, double>();
            foreach (var (key, wt) in _weights)
            {
                if (predicate(wt))
                {
                    newDict[key] = _qDict[key];
                    newWeights[key] = wt;
                }
            }
        }
        else
        {
            // unweighted => each key has weight=1
            bool keep = predicate(1.0);
            if (keep)
            {
                foreach (var key in _qDict.Keys)
                    newDict[key] = _qDict[key];
            }
        }

        var e = new Eigenstates<T>(newDict, _ops);
        e._weights = newWeights;
        return e;
    }

    /// <summary>
    /// Collapse the superposition by crowning the one true value.
    /// It's democracy, but weighted and quantum, so basically rigged.
    /// </summary>
    public T CollapseWeighted()
    {
        if (!States.Any())
            throw new InvalidOperationException("No states available to collapse.");

        if (!IsWeighted)
        {
            // fallback
            return _qDict.Keys.First();
        }
        else
        {
            var (key, _) = _weights!
                .OrderByDescending(x => x.Value)
                .First();
            return key;
        }
    }

    /// <summary>
    /// Sample from your state space in a way that introduces just enough chaos to ruin reproducibility.
    /// </summary>
    public T SampleWeighted(Random? rng = null)
    {
        if (!States.Any())
            throw new InvalidOperationException("No states to sample.");

        rng ??= Random.Shared;

        if (!IsWeighted)
        {
            return _qDict.Keys.First();
        }

        double total = _weights!.Values.Sum();
        if (total <= 1e-15)
        {
            // If all zero, fallback
            return _qDict.Keys.First();
        }

        double roll = rng.NextDouble() * total;
        double cumulative = 0.0;
        foreach (var kvp in _weights)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
                return kvp.Key;
        }

        // fallback
        return _weights.Last().Key;
    }

    private static readonly double _tolerance = 1e-12;

    /// <summary>
    /// Checks if two Eigenstates are truly the same deep down—or just pretending.
    /// Includes an existential tolerance value.
    /// </summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Eigenstates<T> other) return false;

        // Compare keys
        var myKeys = _qDict.Keys.ToHashSet();
        var otherKeys = other._qDict.Keys.ToHashSet();
        if (!myKeys.SetEquals(otherKeys))
            return false;

        // Compare mapped values
        foreach (var k in myKeys)
        {
            if (!EqualityComparer<T>.Default.Equals(_qDict[k], other._qDict[k]))
                return false;
        }

        // If neither is weighted, done
        if (!this.IsWeighted && !other.IsWeighted)
            return true;

        // Otherwise, compare weights
        foreach (var k in myKeys)
        {
            double w1 = 1.0, w2 = 1.0;
            if (_weights != null && _weights.TryGetValue(k, out var wt1)) w1 = wt1;
            if (other._weights != null && other._weights.TryGetValue(k, out var wt2)) w2 = wt2;

            if (Math.Abs(w1 - w2) > _tolerance)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Makes a unique hash that somehow encodes the weight of your guilt, I mean, states.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            // Sort keys so hashing is stable
            foreach (var k in _qDict.Keys.OrderBy(x => x))
            {
                hash = hash * 23 + (k?.GetHashCode() ?? 0);
                // incorporate the mapped value
                hash = hash * 23 + (_qDict[k]?.GetHashCode() ?? 0);

                if (IsWeighted)
                {
                    double w = _weights != null && _weights.TryGetValue(k, out var ww) ? ww : 1.0;
                    long bits = BitConverter.DoubleToInt64Bits(Math.Round(w, 12));
                    hash = hash * 23 + bits.GetHashCode();
                }
            }
            return hash;
        }
    }

    /// <summary>
    /// Returns a quick stats rundown so you can pretend you have control.
    /// </summary>
    public string WeightSummary()
    {
        if (!IsWeighted) return "Weighted: false";
        double sum = _weights!.Values.Sum();
        double max = _weights.Values.Max();
        double min = _weights.Values.Min();
        return $"Weighted: true, Sum: {sum}, Max: {max}, Min: {min}";
    }

    /// <summary>
    /// Applies new weights to the same old states. 
    /// Like giving your data a glow-up without changing its personality.
    /// </summary>
    public Eigenstates<T> WithWeights(Dictionary<T, double> weights)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));

        var filtered = new Dictionary<T, double>();
        foreach (var kvp in weights)
        {
            if (_qDict.ContainsKey(kvp.Key))
                filtered[kvp.Key] = kvp.Value;
        }

        // Create a new instance with the same key->value map, same ops
        var newEigen = new Eigenstates<T>(new Dictionary<T, T>(_qDict), _ops)
        {
            _weights = filtered,
            _eType = this._eType  // preserve the same quantum state type
        };
        return newEigen;
    }
}

#endregion
