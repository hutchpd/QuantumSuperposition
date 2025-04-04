﻿using System;
using System.Numerics;

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

/// <summary>
/// A common interface for all qubits that participate in an entangled system. 
/// It acts as a “handle” that the QuantumSystem can use to communicate with individual qubits.
/// </summary>
public interface IQuantumReference
{
    /// <summary>
    /// Returns the indices within the global quantum system this qubit spans.
    /// For example, a single qubit may be index 0, or a 2-qubit register may be [0,1].
    /// </summary>
    int[] GetQubitIndices();

    /// <summary>
    /// Called by QuantumSystem after a collapse or update.
    /// Used to refresh local views, cached states, or flags like IsCollapsed.
    /// </summary>
    void NotifyWavefunctionCollapsed(Guid collapseId);

    /// <summary>
    /// Whether this reference is currently collapsed.
    /// </summary>
    bool IsCollapsed { get; }

    /// <summary>
    /// Gets the current observed value, if already collapsed.
    /// Otherwise throws or returns default depending on implementation.
    /// </summary>
    object? GetObservedValue();

    /// <summary>
    /// Collapse this reference and return a value from the superposition.
    /// The return type is object for type-agnostic access. Use typed qubits to get T.
    /// </summary>
    object Observe(Random? rng = null);

    /// <summary>
    /// Applies a unitary gate matrix (e.g. Hadamard, Pauli-X) to the local qubit state.
    /// Or subspace of the global wavefunction if entangled.
    /// </summary>
    void ApplyLocalUnitary(Complex[,] gate);
}



/// <summary>
/// Like a gym trainer, but for ints. Does the usual heavy lifting.
/// </summary>
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

/// <summary>
/// Like a quantum therapist, but for complex numbers. Helps them add, subtract, and multiply their feelings.
/// </summary>
public class ComplexOperators : IQuantumOperators<Complex>
{
    public Complex Add(Complex a, Complex b) => a + b;
    public Complex Subtract(Complex a, Complex b) => a - b;
    public Complex Multiply(Complex a, Complex b) => a * b;
    public Complex Divide(Complex a, Complex b) => a / b;

    // Modulo isn't mathematically defined for Complex, so we throw if attempted
    public Complex Mod(Complex a, Complex b) => throw new NotSupportedException("Modulus not supported for complex numbers.");

    // Comparisons on complex numbers are ambiguous, but we’ll define some heuristics based on magnitude:
    public bool GreaterThan(Complex a, Complex b) => a.Magnitude > b.Magnitude;
    public bool GreaterThanOrEqual(Complex a, Complex b) => a.Magnitude >= b.Magnitude;
    public bool LessThan(Complex a, Complex b) => a.Magnitude < b.Magnitude;
    public bool LessThanOrEqual(Complex a, Complex b) => a.Magnitude <= b.Magnitude;
    public bool Equal(Complex a, Complex b) => a == b;
    public bool NotEqual(Complex a, Complex b) => a != b;
}


/// <summary>
/// Currently supports int, and complex numbers. Futer support may include irrational hope, and emotional baggage.
/// </summary>
public static class QuantumOperatorsFactory
{
    public static IQuantumOperators<T> GetOperators<T>()
    {
        if (typeof(T) == typeof(int))
            return (IQuantumOperators<T>)(object)new IntOperators();

        if (typeof(T) == typeof(Complex))
            return (IQuantumOperators<T>)(object)new ComplexOperators();

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

/// <summary>
/// static class to hold basis transforms (e.g. Hadamard):
/// </summary>
public static class QuantumBasis
{
    public static Complex[,] Hadamard => new Complex[,]
    {
        { 1 / Math.Sqrt(2),  1 / Math.Sqrt(2) },
        { 1 / Math.Sqrt(2), -1 / Math.Sqrt(2) }
    };

    public static Complex[,] Identity => new Complex[,]
    {
        { 1, 0 },
        { 0, 1 }
    };

    public static Complex[] ApplyBasis(Complex[] amplitudes, Complex[,] basisMatrix)
    {
        int dim = amplitudes.Length;
        Complex[] result = new Complex[dim];

        for (int i = 0; i < dim; i++)
        {
            result[i] = Complex.Zero;
            for (int j = 0; j < dim; j++)
            {
                result[i] += basisMatrix[i, j] * amplitudes[j];
            }
        }

        return result;
    }
}

public class QuantumSystem
{
    private Dictionary<int[], Complex> _amplitudes = new Dictionary<int[], Complex>();
    private readonly List<IQuantumReference> _registered = new();

    /// <summary>
    /// Optionally construct the system with explicit amplitudes.
    /// </summary>

    public QuantumSystem(Dictionary<int[], Complex>? initialAmps = null)

    {
        if (initialAmps != null)
        {

            _amplitudes = initialAmps;
            NormaliseAmplitudes();
        }
    }

    public void Register(IQuantumReference qubitRef)
    {
        _registered.Add(qubitRef);
    }

    /// <summary>
    /// A simple “observe everything fully” method.
    /// TODO: for partial measurement, expand this to do partial sums.
    /// </summary>
    /// <param name="qubitIndices"></param>
    /// <param name="rng"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public int[] ObserveGlobal(int[] qubitIndices, Random rng)
    {
        if (qubitIndices == null || qubitIndices.Length == 0)
            throw new ArgumentNullException(nameof(qubitIndices));

        // Group amplitudes by projected values on qubitIndices
        var projectionGroups = new Dictionary<int[], List<(int[] state, Complex amplitude)>>(new IntArrayComparer());

        foreach (var kvp in _amplitudes)
        {
            var fullState = kvp.Key;
            var projected = qubitIndices.Select(i => fullState[i]).ToArray();

            if (!projectionGroups.ContainsKey(projected))
                projectionGroups[projected] = new();

            projectionGroups[projected].Add((fullState, kvp.Value));
        }

        // Step 2: Compute total probabilities for each projection group
        var probSums = projectionGroups.ToDictionary(
            g => g.Key,
            g => g.Value.Sum(x => x.amplitude.Magnitude * x.amplitude.Magnitude),
            new IntArrayComparer()
        );

        double totalProb = probSums.Values.Sum();
        if (totalProb <= 1e-15)
            throw new InvalidOperationException("All probabilities are zero or wavefunction is empty.");

        // Sample a group based on probability
        double roll = rng.NextDouble() * totalProb;
        double cumulative = 0.0;

        int[] chosenProjection = Array.Empty<int>();
        foreach (var (proj, p) in probSums)
        {
            cumulative += p;
            if (roll <= cumulative)
            {
                chosenProjection = proj;
                break;
            }
        }

        // Retain only amplitudes matching the chosen projection
        var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());
        foreach (var (state, amp) in projectionGroups[chosenProjection])
        {
            newAmps[state] = amp;
        }

        _amplitudes = newAmps;

        // Renormalise
        NormaliseAmplitudes();

        // Notify
        var collapseId = Guid.NewGuid();
        foreach (var refQ in _registered)
        {
            refQ.NotifyWavefunctionCollapsed(collapseId);
        }

        // Return observed values
        return chosenProjection;
    }




    /// <summary>
    /// ApplyTwoQubitGate function.
    /// </summary>
    public void ApplyTwoQubitGate(int qubitA, int qubitB, Complex[,] gate)
    {
        if (gate.GetLength(0) != 4 || gate.GetLength(1) != 4)
            throw new ArgumentException("Gate must be a 4x4 matrix.");

        // 1. Group basis states by fixed values of all qubits *except* qubitA and qubitB
        var grouped = new Dictionary<string, List<int[]>>();

        foreach (var state in _amplitudes.Keys.ToList())
        {
            // Build a string key excluding qubitA and qubitB (used to group together the 4 basis states)
            var key = string.Join(",", state.Select((val, idx) => (idx != qubitA && idx != qubitB) ? val.ToString() : "*"));

            if (!grouped.ContainsKey(key))
                grouped[key] = new();

            grouped[key].Add(state);
        }

        var newAmplitudes = new Dictionary<int[], Complex>(new IntArrayComparer());

        // 2. Process each group (should contain exactly 4 states representing the 2 qubits)
        foreach (var group in grouped.Values)
        {
            // Build a 4-vector for current basis states
            var basisMap = new Dictionary<(int, int), int[]>();
            var vec = new Complex[4];

            foreach (var basis in group)
            {
                int a = basis[qubitA];
                int b = basis[qubitB];
                int index = (a << 1) | b; // binary encoding: 00, 01, 10, 11 → 0–3

                vec[index] = _amplitudes.TryGetValue(basis, out var amp) ? amp : Complex.Zero;
                basisMap[(a, b)] = basis;
            }

            // 3. Apply the gate: newVec = gate * vec
            var newVec = new Complex[4];
            for (int i = 0; i < 4; i++)
            {
                newVec[i] = Complex.Zero;
                for (int j = 0; j < 4; j++)
                {
                    newVec[i] += gate[i, j] * vec[j];
                }
            }

            // 4. Store updated amplitudes back in _amplitudes
            foreach (var ((a, b), state) in basisMap)
            {
                int idx = (a << 1) | b;
                newAmplitudes[state] = newVec[idx];
            }
        }

        _amplitudes = newAmplitudes;
        NormaliseAmplitudes();
    }

    public void ApplySingleQubitGate(int qubit, Complex[,] gate)
    {
        if (gate.GetLength(0) != 2 || gate.GetLength(1) != 2)
            throw new ArgumentException("Single-qubit gate must be a 2x2 matrix.");

        var newAmps = new Dictionary<int[], Complex>(new IntArrayComparer());

        foreach (var state in _amplitudes.Keys.ToList())
        {
            int bit = state[qubit];

            var basis0 = (int[])state.Clone(); basis0[qubit] = 0;
            var basis1 = (int[])state.Clone(); basis1[qubit] = 1;

            Complex a0 = _amplitudes.TryGetValue(basis0, out var amp0) ? amp0 : Complex.Zero;
            Complex a1 = _amplitudes.TryGetValue(basis1, out var amp1) ? amp1 : Complex.Zero;

            Complex newAmp = gate[bit, 0] * a0 + gate[bit, 1] * a1;

            newAmps[state] = newAmp;
        }

        _amplitudes = newAmps;
        NormaliseAmplitudes();
    }


    private void NormaliseAmplitudes()
    {
        double total = _amplitudes.Values.Sum(a => a.Magnitude * a.Magnitude);
        if (total < 1e-15)
        {
            // All zero => do nothing or reset
            return;
        }
        double norm = Math.Sqrt(total);
        foreach (var k in _amplitudes.Keys.ToList())
        {
            _amplitudes[k] /= norm;
        }
    }

    /// <summary>
    /// Exposes the dictionary for debugging or advanced usage.
    /// Be careful: direct modifications can break normalization.
    /// </summary>
    public IReadOnlyDictionary<int[], Complex> Amplitudes => _amplitudes;
}

public class IntArrayComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (x == null || y == null) return false;
        return x.SequenceEqual(y);
    }

    public int GetHashCode(int[] obj)
    {
        unchecked
        {
            int hash = 17;
            foreach (int val in obj)
            {
                hash = hash * 31 + val;
            }
            return hash;
        }
    }
}

#endregion

#region QuantumConfig

public static class QuantumConfig
{
    // Set to true to disallow collapse if the resulting value equals default(T).
    public static bool ForbidDefaultOnCollapse { get; set; } = true;
}

#endregion

#region QuBit<T> Implementation

/// <summary>
/// QuBit<T> represents a superposition of values of type T, optionally weighted.
/// It's like Schrödinger's inbox: everything's unread and somehow also read.
/// </summary>
public partial class QuBit<T> : IQuantumReference
{
    private readonly Func<T, bool> _valueValidator;
    private bool _isFrozen => _isActuallyCollapsed;
    public bool IsInSuperposition => _eType == QuantumStateType.Disjunctive && !_isActuallyCollapsed;

    private readonly QuantumSystem? _system;    // if non-null, this qubit is in a shared system
    private readonly int[] _qubitIndices;       // which qubit(s) in that system we represent

    // For IQuantumReference:

    private bool _isCollapsedFromSystem;        // if the system has collapsed on us
    private object? _systemObservedValue;

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

        // This qubit is in superposition until a collapse occurs
        _eType = QuantumStateType.Disjunctive;

    }



    // If we have no quantum system, we fallback to local states:
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

    public static QuBit<T> Superposed(IEnumerable<T> states)
    {
        var qubit = new QuBit<T>(states);
        var distinctCount = qubit.States.Distinct().Count();
        if (distinctCount > 1)
            qubit._eType = QuantumStateType.Disjunctive;
        return qubit;
    }

    // Main constructor for unweighted items (local useage)
    public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qList = Items;

        if (_qList.Distinct().Count() > 1)
            _eType = QuantumStateType.Disjunctive;

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
            SetType(QuantumStateType.Disjunctive);

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
            SetType(QuantumStateType.Disjunctive);

        // no system
        _system = null;
        _qubitIndices = Array.Empty<int>();
        _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);
    }


    #endregion


    #region State Type Helpers

    // Guard to ensure mutable operations are only allowed when not collapsed.
    private void EnsureMutable()
    {
        if (_isFrozen)
            throw new InvalidOperationException("Cannot modify a collapsed QuBit.");
    }

    private void SetType(QuantumStateType t)
    {
        EnsureMutable();
        _eType = t;
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
            SetType(QuantumStateType.Disjunctive);

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
    /// Creates a mutable clone of the QuBit – copying current states and weights,
    /// but resetting the collapsed (immutable) flag so you can start over.
    /// </summary>
    public QuBit<T> CloneMutable()
    {
        var clonedWeights = _weights != null ? new Dictionary<T, Complex>(_weights) : null;
        var clonedList = _qList.ToList();
        var clone = new QuBit<T>(clonedList, clonedWeights, _ops, _valueValidator)
        {
            _eType = this._eType
            // Note: collapse-related fields are not copied, so the clone is mutable.
        };
        return clone;
    }

    #endregion

    #region Fields for Local Storage (used if _system == null)

    private IEnumerable<T> _qList;
    private Dictionary<T, Complex>? _weights;
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops = QuantumOperatorsFactory.GetOperators<T>();
    private static readonly IQuantumOperators<T> _defaultOps = QuantumOperatorsFactory.GetOperators<T>();

    // Track the last seed (if used) and a unique collapse ID
    private Guid? _collapseHistoryId;
    private int? _lastCollapseSeed;

    // For debugging or replay purposes, you can track the last collapse history ID and seed.
    public Guid? LastCollapseHistoryId => _collapseHistoryId;
    public int? LastCollapseSeed => _lastCollapseSeed;

    // Fields for supporting mock collapse
    private bool _mockCollapseEnabled;
    private T? _mockCollapseValue;

    // Track actual collapse
    private bool _isActuallyCollapsed;
    private T? _collapsedValue;

    public IReadOnlyCollection<T> States =>
        _weights != null ? (IReadOnlyCollection<T>)_weights.Keys : _qList.ToList();

    public IQuantumOperators<T> Operators => _ops;

    #endregion
    #region State Type Helpers
    // Lets you toggle between 'All must be true', 'Any might be true', and 'Reality is now a lie'.
    // Think of it like mood settings, but for wavefunctions.
    // Also, it helps you avoid existential crises by keeping track of your quantum state.

    public QuantumStateType GetCurrentType() => _eType;

   

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

        Dictionary<T, Complex>? newWeights = null;
        if (a._weights != null || b._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
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

        Dictionary<T, Complex>? newWeights = null;
        if (a._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
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

        Dictionary<T, Complex>? newWeights = null;
        if (b._weights != null)
        {
            newWeights = new Dictionary<T, Complex>();
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

    #region Evaluation / Collapse & Introspection Enhancements
    // Reality check: converts quantum indecision into a final verdict.
    // Basically forces the whole wavefunction to agree like it’s group therapy for particles.
    public bool EvaluateAll()
    {
        SetType(QuantumStateType.Conjunctive);

        // More descriptive exception if empty
        if (!States.Any())
        {
            throw new InvalidOperationException(
                $"No states to evaluate. IsCollapsed={_isActuallyCollapsed}, StateCount={States.Count}, Type={_eType}."
            );
        }

        return States.All(state => !EqualityComparer<T>.Default.Equals(state, default(T)));
    }

    /// <summary>
    /// Observes (collapses) the QuBit with an optional Random instance for deterministic replay.
    /// If the QuBit is already collapsed, returns the previously collapsed value.
    /// If mock-collapse is enabled, returns the forced value without changing the underlying state.
    /// </summary>
    /// <param name="rng">Optional random instance for deterministic replay.</param>
    /// <returns>The collapsed (observed) value.</returns>
    public T Observe(Random? rng = null)
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

        // Perform a real probabilistic collapse using weighted sampling
        rng ??= Random.Shared;
        // We only know the seed if user gave us one in Observe(int seed)
        // But do assign a new collapseHistoryId for debugging
        _collapseHistoryId = Guid.NewGuid();

        T picked = SampleWeighted(rng);

        // Use the configuration flag and value validator to protect against default(T)
        if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
        {
            throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
        }

        // log whatever we get default(T)
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
            _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
        }
        SetType(QuantumStateType.CollapsedResult);

        return picked;
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

    public T ObserveInBasis(Complex[,] basis, Random? rng = null)
    {
        if (_weights == null || _weights.Count != 2)
            throw new InvalidOperationException("Basis transformations currently only support 2-state qubits.");

        var states = _weights.Keys.ToArray();
        var amplitudes = states.Select(s => _weights[s]).ToArray();

        // Apply the basis transform
        var transformed = QuantumBasis.ApplyBasis(amplitudes, basis);

        // Rebuild a new qubit with these amplitudes
        var newWeights = new Dictionary<T, Complex>
        {
            [states[0]] = transformed[0],
            [states[1]] = transformed[1]
        };

        return new QuBit<T>(states, newWeights, _ops).Observe(rng);
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

    // ---------------------------------------------------------------------
    // Introspection / Utilities
    // ---------------------------------------------------------------------

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

    private bool _weightsAreNormalized = false;

    /// <summary>
    /// Normalises the amplitudes of the QuBit so that the sum of their squared magnitudes equals 1.
    /// </summary>
    public void NormaliseWeights()
    {
        if (_weights == null || _weightsAreNormalized) return;

        // Compute the sum of the squared magnitudes (|a|²)
        double totalProbability = _weights.Values
            .Select(a => a.Magnitude * a.Magnitude)
            .Sum();

        if (totalProbability <= double.Epsilon)
            return; // avoid division by zero

        // Scale each amplitude by 1 / sqrt(totalProbability)
        double normFactor = Math.Sqrt(totalProbability);

        foreach (var key in _weights.Keys.ToList())
        {
            _weights[key] /= normFactor;
        }
    }


    /// <summary>
    /// An explicit “WithNormalizedWeights()” 
    /// that returns a new QuBit or modifies in place. Here we clone for safety.
    /// </summary>
    public QuBit<T> WithNormalizedWeights()
    {
        if (!IsWeighted) return this; // no weights to normalize



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
    /// Optionally, modify WithWeights(...) to auto-normalize if desired:
    /// </summary>

    public QuBit<T> WithWeights(Dictionary<T, Complex> weights, bool autoNormalize = false)
    {

        if (weights == null) throw new ArgumentNullException(nameof(weights));

        // Gather only the weights for keys we already have in States.

        var filtered = new Dictionary<T, Complex>();

        foreach (var kvp in weights)
        {
            if (States.Contains(kvp.Key))
                filtered[kvp.Key] = kvp.Value;
        }

        var newQ = new QuBit<T>(_qList, filtered, _ops)
        {
            _eType = this._eType
        };

        if (autoNormalize)
            newQ.NormaliseWeights();

        return newQ;
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
    // Does a little dance, rolls a quantum die, picks a value.
    // May cause minor existential dread or major debugging regrets.
    /// </summary>
    public T SampleWeighted(Random? rng = null)
    {
        if (!States.Any())
            throw new InvalidOperationException("No states available to sample.");

        rng ??= Random.Shared;

        if (!IsWeighted)
        {
            return ToCollapsedValues().First();
        }
        else
        {
            NormaliseWeights();
        }

        // Use squared magnitude for probability
        var probabilities = _weights!.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Magnitude * kvp.Value.Magnitude
        );

        double totalProb = probabilities.Values.Sum();
        if (totalProb <= 1e-15)
        {
            // All zero probabilities – fallback
            return ToCollapsedValues().First();
        }

        double roll = rng.NextDouble() * totalProb;
        double cumulative = 0.0;

        foreach (var (key, prob) in probabilities)
        {
            cumulative += prob;
            if (roll <= cumulative)
                return key;
        }

        // Fallback in case of rounding issues
        return probabilities.Last().Key;
    }

    /// <summary>
    /// Tolerance used for comparing weights in equality checks.
    /// This allows for minor floating-point drift.
    /// </summary>
    private static readonly double _tolerance = 1e-9;

    /// <summary>
    /// Compares two QuBits with the grace of a therapist and the precision of a passive-aggressive spreadsheet.
    /// Compare Squared Magnitudes (Probabilistic Equality)
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not QuBit<T> other) return false;

        var mySet = States.Distinct().ToHashSet();
        var otherSet = other.States.Distinct().ToHashSet();
        if (!mySet.SetEquals(otherSet)) return false;

        if (!this.IsWeighted && !other.IsWeighted)
            return true;

        foreach (var s in mySet)
        {
            double p1 = 1.0, p2 = 1.0;
            if (_weights != null && _weights.TryGetValue(s, out var amp1)) p1 = amp1.Magnitude * amp1.Magnitude;
            if (other._weights != null && other._weights.TryGetValue(s, out var amp2)) p2 = amp2.Magnitude * amp2.Magnitude;
            if (Math.Abs(p1 - p2) > _tolerance) return false;
        }

        return true;
    }

    /// <summary>
    /// Compares two QuBits with the grace of a therapist and the precision of a passive-aggressive spreadsheet.
    /// Compare Complex Amplitudes (Strict State Equality, as opposed to Probabilistic Equality)
    /// </summary>
    public bool StrictlyEquals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not QuBit<T> other) return false;

        var mySet = States.Distinct().ToHashSet();
        var otherSet = other.States.Distinct().ToHashSet();
        if (!mySet.SetEquals(otherSet)) return false;

        if (!this.IsWeighted && !other.IsWeighted)
            return true;

        foreach (var s in mySet)
        {
            Complex a1 = Complex.Zero, a2 = Complex.Zero;
            if (_weights != null && _weights.TryGetValue(s, out var amp1)) a1 = amp1;
            if (other._weights != null && other._weights.TryGetValue(s, out var amp2)) a2 = amp2;

            if ((a1 - a2).Magnitude > _tolerance) return false;
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
        // This is called by the QuantumSystem if a global collapse occurs.
        // We'll set a flag so we know we can't do local modifications now.
        _isCollapsedFromSystem = true;
        // We could also store the collapseId, in case we want to track it
        // or do something with it in logs.
        _collapseHistoryId = collapseId;
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

            // Measure only the qubits we span
            int[] measured = _system.ObserveGlobal(_qubitIndices, rng);

            object result;

            if (typeof(T) == typeof(int))
            {
                // If this qubit spans one qubit, return the single value
                result = measured.Length == 1 ? measured[0] : measured;
            }
            else if (typeof(T) == typeof(bool))
            {
                // Interpret 0 => false, 1 => true (crude, but sufficient if binary)
                result = measured.Length == 1 ? measured[0] != 0 : measured.Select(v => v != 0).ToArray();
            }
            else if (typeof(T) == typeof(int[]))
            {
                result = measured;
            }
            else
            {
                // fallback: return raw int[]
                result = measured;
            }

            _systemObservedValue = result;
            return result;
        }

        // Fall back to local collapse
        return ObserveLocal(rng);
    }


    public void ApplyLocalUnitary(Complex[,] gate)
    {
        if (_system != null)
        {
            if (_qubitIndices.Length == 1 && gate.GetLength(0) == 2 && gate.GetLength(1) == 2)
            {
                // Apply single-qubit gate
                _system.ApplySingleQubitGate(_qubitIndices[0], gate);
            }
            else if (_qubitIndices.Length == 2 && gate.GetLength(0) == 4 && gate.GetLength(1) == 4)
            {
                // Apply two-qubit gate
                _system.ApplyTwoQubitGate(_qubitIndices[0], _qubitIndices[1], gate);
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

        var transformed = QuantumBasis.ApplyBasis(amplitudes, gate);

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
#endregion

#region Eigenstates<T> Implementation

/// <summary>
/// Eigenstates<T> preserves original input keys in a dictionary Key->Value,
/// because sometimes you just want your quantum states to stop changing the subject.
/// </summary>
public class Eigenstates<T>
{
    private readonly Func<T, bool> _valueValidator = v => !EqualityComparer<T>.Default.Equals(v, default);

    private Dictionary<T, T> _qDict;
    private Dictionary<T, Complex>? _weights; // optional weighting
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops;

    // Track the last seed (if used) and a unique collapse ID
    private Guid? _collapseHistoryId;
    private int? _lastCollapseSeed;
    private bool _isActuallyCollapsed;
    private T? _collapsedValue;

    public IReadOnlyCollection<T> States => _qDict.Keys;

    #region Constructors
    // Constructors that let you map values to themselves or to other values.
    // Also doubles as a safe space for anyone scared of full superposition commitment.

    public Eigenstates(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        // same weight, then the waveform should collapse to the same value
        if (Items == null) throw new ArgumentNullException(nameof(Items));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qDict = Items.Distinct().ToDictionary(x => x, x => x);
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

    public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems, IQuantumOperators<T> ops)
    {
        if (weightedItems == null) throw new ArgumentNullException(nameof(weightedItems));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));

        var dict = new Dictionary<T, Complex>();
        foreach (var (val, w) in weightedItems)
        {
            if (!dict.ContainsKey(val))
                dict[val] = 0.0;
            dict[val] += w;
        }

        _qDict = dict.Keys.ToDictionary(x => x, x => x);
        _weights = dict;
    }

    public Eigenstates(IEnumerable<(T value, Complex weight)> weightedItems)
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
        var newWeights = new Dictionary<T, Complex>();

        // Loop through all weighted values from both operands.
        foreach (var (valA, wA) in a.ToMappedWeightedValues())
        {
            foreach (var (valB, wB) in b.ToMappedWeightedValues())
            {
                var newValue = op(valA, valB);
                Complex combinedWeight = wA * wB;
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
        Dictionary<T, Complex>? newWeights = null;

        if (a._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in a._qDict)
        {
            var newVal = op(kvp.Value, b);
            result[kvp.Key] = newVal;  // retain original key, update value

            if (newWeights != null)
            {
                Complex wA = a._weights != null && a._weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                newWeights[kvp.Key] = wA;  // use original key
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
        Dictionary<T, Complex>? newWeights = null;

        if (b._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in b._qDict)
        {
            var newVal = op(a, kvp.Value);
            result[kvp.Key] = newVal;  // retain original key, update value

            if (newWeights != null)
            {
                Complex wB = b._weights != null && b._weights.TryGetValue(kvp.Key, out var bw) ? bw : 1.0;
                newWeights[kvp.Key] = wB;  // use original key
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
        Dictionary<T, Complex>? newWeights = null;

        if (_weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in _qDict)
        {
            if (condition(kvp.Value, value))
            {
                result[kvp.Key] = kvp.Value;
                if (newWeights != null)
                {
                    Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
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
        Dictionary<T, Complex>? newWeights = null;

        if (_weights != null || other._weights != null)
            newWeights = new Dictionary<T, Complex>();

        foreach (var kvp in _qDict)
        {
            foreach (var kvp2 in other._qDict)
            {
                if (condition(kvp.Value, kvp2.Value))
                {
                    result[kvp.Key] = kvp.Value;
                    if (newWeights != null)
                    {
                        Complex wA = _weights != null && _weights.TryGetValue(kvp.Key, out var aw) ? aw : 1.0;
                        Complex wB = other._weights != null && other._weights.TryGetValue(kvp2.Key, out var bw) ? bw : 1.0;
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

    /// <summary>
    /// Returns a string representation of the Eigenstates.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var distinctKeys = _qDict.Keys.Distinct().ToList();
        if (_weights == null || AllWeightsEqual(_weights))
        {
            // Return just the element if there is a single unique state
            if (distinctKeys.Count == 1)
                return distinctKeys.First().ToString();

            return _eType == QuantumStateType.Disjunctive
                ? $"any({string.Join(", ", distinctKeys)})"
                : $"all({string.Join(", ", distinctKeys)})";
        }
        else
        {
            var pairs = ToMappedWeightedValues().Select(x => $"{x.value}:{x.weight}");
            return _eType == QuantumStateType.Disjunctive
                ? $"any({string.Join(", ", pairs)})"
                : $"all({string.Join(", ", pairs)})";
        }
    }

    /// <summary>
    /// Returns a collection of (mapped value, weight) pairs,
    /// where weights correspond to the original input keys.
    /// </summary>
    public IEnumerable<(T value, Complex weight)> ToMappedWeightedValues()
    {
        if (_weights == null)
        {
            foreach (var k in _qDict.Keys.Distinct())
                yield return (_qDict[k], 1.0);
        }
        else
        {
            foreach (var kvp in _weights)
                yield return (_qDict[kvp.Key], kvp.Value);
        }
    }

    private bool _weightsAreNormalized = false;

    /// <summary>
    /// Normalises the weights of the Eigenstates.
    /// </summary>
    public void NormaliseWeights()
    {
        if (_weights == null || _weightsAreNormalized) return;

        // Compute the sum of the squared magnitudes (|a|²)
        double totalProbability = _weights.Values
            .Select(a => a.Magnitude * a.Magnitude)
            .Sum();

        if (totalProbability <= double.Epsilon)
            return; // avoid division by zero

        // Scale each amplitude by 1 / sqrt(totalProbability)
        double normFactor = Math.Sqrt(totalProbability);

        foreach (var key in _weights.Keys.ToList())
        {
            _weights[key] /= normFactor;
        }
    }

    /// <summary>
    /// Checks if all weights are equal. If so, congratulations — your data achieved perfect balance, Thanos-style.
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
    /// Checks if all weights are probably equal (i.e., squared magnitudes).
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

    /// <summary>
    /// Returns a string representation of the Eigenstates,
    /// perfect for when your code works and you still don’t know why.
    /// </summary>
    /// <returns></returns>
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
                    Complex w = _weights.TryGetValue(kvp.Key, out var val) ? val : 1.0;
                    return $"{kvp.Key} => {kvp.Value} (weight: {w})";
                }));
        }
    }

    #endregion

    #region Observation & Collapse, Mocking, Introspection

    private bool _mockCollapseEnabled;
    private T? _mockCollapseValue;
    public Guid? CollapseHistoryId => _collapseHistoryId;
    public int? LastCollapseSeed => _lastCollapseSeed;


    /// <summary>
    /// Observes (collapses) the Eigenstates with an optional random instance
    /// If mock collapse is enabled, it will return the forced value without changing state.
    /// Otherwise, perform a real probabilistic collapse.
    /// </summary>
    public T Observe(Random? rng = null)
    {
        if (_mockCollapseEnabled)
        {
            if (_mockCollapseValue == null)
                throw new InvalidOperationException("Mock collapse enabled but no mock value is set.");
            return _mockCollapseValue;
        }

        if (_isActuallyCollapsed && _collapsedValue != null && _eType == QuantumStateType.CollapsedResult)
        {
            return _collapsedValue;
        }

        rng ??= Random.Shared;

        // If this was not called via Observe(int seed), then we need to generate a new default collapse ID.
        _collapseHistoryId ??= Guid.NewGuid();

        T picked = SampleWeighted(rng);

        if (QuantumConfig.ForbidDefaultOnCollapse && !_valueValidator(picked))
        {
            throw new InvalidOperationException("Collapse resulted in default(T), which is disallowed by config.");
        }

        _collapsedValue = picked;
        _isActuallyCollapsed = true;
        var newDict = new Dictionary<T, T> { { picked, picked } };
        _qDict = newDict;
        if (_weights != null)
        {
            _weights = new Dictionary<T, Complex> { { picked, 1.0 } };
        }
        _eType = QuantumStateType.CollapsedResult;

        return picked;
    }

    /// <summary>
    /// Observes (collapses) using a supplied seed for deterministic behavior.
    /// </summary>
    public T Observe(int seed)
    {
        _lastCollapseSeed = seed;
        _collapseHistoryId = Guid.NewGuid();
        var rng = new Random(seed);
        return Observe(rng);
    }

    /// <summary>
    /// Enables mock collapse for Eigenstates, forcing Observe() to return forcedValue.
    /// </summary>
    public Eigenstates<T> WithMockCollapse(T forcedValue)
    {
        _mockCollapseEnabled = true;
        _mockCollapseValue = forcedValue;
        return this;
    }

    /// <summary>
    /// Disables mock collapse for Eigenstates.
    /// </summary>
    public Eigenstates<T> WithoutMockCollapse()
    {
        _mockCollapseEnabled = false;
        _mockCollapseValue = default;
        return this;
    }

    /// <summary>
    /// Returns a string representation of the current eigenstates without collapsing.
    /// </summary>
    public string show_states()
    {
        return ToDebugString();
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
            .OrderByDescending(kvp => kvp.Value.Magnitude * kvp.Value.Magnitude)
            .Take(n)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Filter your states by weight like you're trimming down party invites.
    /// Only show up if your weight is greater than 0.75, Chad.”
    /// </summary>
    public Eigenstates<T> FilterByProbability(Func<Complex, bool> predicate)
    {
        var newDict = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (IsWeighted)
        {
            newWeights = new Dictionary<T, Complex>();
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
    /// Filter your states by amplitude like you're picking a favorite child.
    /// Useful if you're interested in real/imaginary structure instead of probability.
    /// </summary>
    /// <param name="amplitudePredicate">A predicate applied directly to the complex amplitude.</param>
    /// <returns>A filtered QuBit containing only states whose amplitude passes the test.</returns>
    public Eigenstates<T> FilterByAmplitude(Func<Complex, bool> amplitudePredicate)
    {
        var newDict = new Dictionary<T, T>();
        Dictionary<T, Complex>? newWeights = null;

        if (IsWeighted)
        {
            newWeights = new Dictionary<T, Complex>();
            foreach (var (key, amp) in _weights!)
            {
                if (amplitudePredicate(amp))
                {
                    newDict[key] = _qDict[key];
                    newWeights[key] = amp;
                }
            }
        }
        else
        {
            // unweighted => treat each amplitude as 1 + 0i
            Complex defaultAmp = Complex.One;
            if (amplitudePredicate(defaultAmp))
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
            var key = _weights!.MaxBy(x => x.Value.Magnitude)!.Key;
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
        else
        {
            NormaliseWeights();
        }

        // Compute total probability (sum of squared magnitudes)
        double totalProb = _weights!.Values.Sum(amp => amp.Magnitude * amp.Magnitude);

        if (totalProb <= 1e-15)
        {
            // All amplitudes are effectively zero — fallback
            return _qDict.Keys.First();
        }

        // Roll a number between 0 and total probability
        double roll = rng.NextDouble() * totalProb;
        double cumulativeProb = 0.0;

        // Accumulate probabilities from amplitudes
        foreach (var (key, amp) in _weights)
        {
            cumulativeProb += amp.Magnitude * amp.Magnitude;
            if (roll <= cumulativeProb)
                return key;
        }

        // Fallback safety
        return _weights.Last().Key;
    }


    /// <summary>
    /// Tolerance used for comparing weights in equality checks.
    /// This allows for minor floating-point drift.
    /// </summary>
    private static readonly double _tolerance = 1e-9;

    /// <summary>
    /// Checks if two Eigenstates are truly the same deep down—or just pretending.
    /// Includes an existential tolerance value.
    /// Compare Squared Magnitudes (Probabilistic Equality)
    /// </summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Eigenstates<T> other) return false;

        var myKeys = _qDict.Keys.ToHashSet();
        var otherKeys = other._qDict.Keys.ToHashSet();
        if (!myKeys.SetEquals(otherKeys)) return false;

        foreach (var k in myKeys)
        {
            if (!EqualityComparer<T>.Default.Equals(_qDict[k], other._qDict[k]))
                return false;
        }

        // If neither is weighted, treat as equal
        if (!IsWeighted && !other.IsWeighted)
            return true;

        // Compare squared magnitudes (probabilities)
        foreach (var k in myKeys)
        {
            Complex a1 = _weights != null && _weights.TryGetValue(k, out var v1) ? v1 : Complex.One;
            Complex a2 = other._weights != null && other._weights.TryGetValue(k, out var v2) ? v2 : Complex.One;

            double prob1 = a1.Magnitude * a1.Magnitude;
            double prob2 = a2.Magnitude * a2.Magnitude;

            if (Math.Abs(prob1 - prob2) > _tolerance)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if two Eigenstates are truly the same deep down—or just pretending.
    /// Includes an existential tolerance value.
    /// Compare Complex Amplitudes (Strict State Equality, as opposed to Probabilistic Equality)
    /// </summary>
    public bool StrictlyEquals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Eigenstates<T> other) return false;

        var myKeys = _qDict.Keys.ToHashSet();
        var otherKeys = other._qDict.Keys.ToHashSet();
        if (!myKeys.SetEquals(otherKeys)) return false;

        foreach (var k in myKeys)
        {
            if (!EqualityComparer<T>.Default.Equals(_qDict[k], other._qDict[k]))
                return false;
        }

        if (!IsWeighted && !other.IsWeighted)
            return true;

        foreach (var k in myKeys)
        {
            Complex a1 = _weights != null && _weights.TryGetValue(k, out var v1) ? v1 : Complex.One;
            Complex a2 = other._weights != null && other._weights.TryGetValue(k, out var v2) ? v2 : Complex.One;

            if ((a1 - a2).Magnitude > _tolerance)
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

            foreach (var k in _qDict.Keys.OrderBy(x => x))
            {
                // Hash the key and its projected value
                hash = hash * 23 + (k?.GetHashCode() ?? 0);
                hash = hash * 23 + (_qDict[k]?.GetHashCode() ?? 0);

                if (IsWeighted && _weights != null && _weights.TryGetValue(k, out var amp))
                {
                    // Round both real and imaginary parts
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
    /// Returns a quick stats rundown so you can pretend you have control.
    /// </summary>
    public string WeightSummary()
    {
        if (!IsWeighted) return "Weighted: false";

        var probs = _weights!.Values.Select(amp => amp.Magnitude * amp.Magnitude).ToList();
        double sum = probs.Sum();
        double max = probs.Max();
        double min = probs.Min();

        return $"Weighted: true (complex amplitudes), Total Prob: {sum:F4}, Max |amp|²: {max:F4}, Min |amp|²: {min:F4}";
    }

    /// <summary>
    /// Applies new weights to the same old states. 
    /// Like giving your data a glow-up without changing its personality.
    /// </summary>
    public Eigenstates<T> WithWeights(Dictionary<T, Complex> weights)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));

        var filtered = new Dictionary<T, Complex>();
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

    public string ToDebugString(bool includeCollapseMetadata = false)
    {
        var baseInfo = _weights == null
            ? string.Join(", ", _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"))
            : string.Join(", ", _qDict.Select(kvp =>
            {
                Complex w = _weights.TryGetValue(kvp.Key, out var val) ? val : 1.0;
                return $"{kvp.Key} => {kvp.Value} (weight: {w})";
            }));

        if (!includeCollapseMetadata) return baseInfo;

        return $"{baseInfo}\nCollapsed: {_isActuallyCollapsed}, " +
               $"Seed: {_lastCollapseSeed}, ID: {_collapseHistoryId}";
    }

}

#endregion