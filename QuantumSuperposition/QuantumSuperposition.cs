using System;
using System.Collections.Generic;
using System.Linq;

#region QuantumCore

public enum QuantumStateType
{
    Conjunctive,      // All states must be true.
    Disjunctive,      // Any state can be true.
    CollapsedResult   // Only one state remains after collapse.
}

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

public static class QuantumOperatorsFactory
{
    public static IQuantumOperators<T> GetOperators<T>()
    {
        if (typeof(T) == typeof(int))
            return (IQuantumOperators<T>)(object)new IntOperators();
        throw new NotImplementedException("Default operators not implemented for type " + typeof(T));
    }
}

// Helper class that factors out repetitive loops for arithmetic.
public static class QuantumMathUtility<T>
{
    public static IEnumerable<T> CombineAll(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, T> op) =>
        a.SelectMany(x => b.Select(y => op(x, y)));

    public static IEnumerable<T> Combine(IEnumerable<T> a, T b, Func<T, T, T> op) =>
        a.Select(x => op(x, b));

    public static IEnumerable<T> Combine(T a, IEnumerable<T> b, Func<T, T, T> op) =>
        b.Select(x => op(a, x));
}

// Core QuBit<T> type that implements arithmetic and filtering.
public partial class QuBit<T>
{
    private IEnumerable<T> _qList;
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops;
    private static readonly IQuantumOperators<T> _defaultOps = QuantumOperatorsFactory.GetOperators<T>();

    public IReadOnlyCollection<T> States => _qList.ToList();
    public IQuantumOperators<T> Operators => _ops;

    public QuBit(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qList = Items;
        if (_qList.Distinct().Count() > 1)
            SetType(QuantumStateType.Disjunctive);
    }

    public QuBit(IEnumerable<T> Items) : this(Items, _defaultOps) { }

    public QuantumStateType GetCurrentType() => _eType;
    private void SetType(QuantumStateType t) => _eType = t;

    public QuBit<T> Append(T element)
    {
        _qList = _qList.Concat(new[] { element });
        return this;
    }

    // Filtering mode setters.
    public QuBit<T> Any() { SetType(QuantumStateType.Disjunctive); return this; }
    public QuBit<T> All() { SetType(QuantumStateType.Conjunctive); return this; }

    // Arithmetic helper methods.
    private QuBit<T> Do_oper_type(QuBit<T> a, QuBit<T> b, Func<T, T, T> op)
    {
        var result = QuantumMathUtility<T>.CombineAll(a._qList, b._qList, op);
        return new QuBit<T>(result, _ops);
    }
    private QuBit<T> Do_oper_type(QuBit<T> a, T b, Func<T, T, T> op)
    {
        var result = QuantumMathUtility<T>.Combine(a._qList, b, op);
        return new QuBit<T>(result, _ops);
    }
    private QuBit<T> Do_oper_type(T a, QuBit<T> b, Func<T, T, T> op)
    {
        var result = QuantumMathUtility<T>.Combine(a, b._qList, op);
        return new QuBit<T>(result, _ops);
    }

    // Arithmetic operator overloads.
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

    // EvaluateAll collapses the superposition to a single Boolean.
    public bool EvaluateAll()
    {
        SetType(QuantumStateType.Conjunctive);
        return _qList.All(state => !EqualityComparer<T>.Default.Equals(state, default(T)));
    }

    public override string ToString()
    {
        if (_eType == QuantumStateType.Disjunctive)
            return $"any({string.Join(", ", _qList.Distinct())})";
        else
        {
            var distinct = _qList.Distinct().ToList();
            return distinct.Count == 1 ? distinct[0].ToString() : $"all({string.Join(", ", distinct)})";
        }
    }

    public IEnumerable<T> ToValues()
    {
        if (!_qList.Any())
            throw new InvalidOperationException("No values to collapse.");
        if (_eType == QuantumStateType.Disjunctive)
            return _qList;
        else if (_eType == QuantumStateType.Conjunctive)
            return _qList.Distinct().Count() == 1 ? new List<T> { _qList.First() } : _qList;
        else
            return new List<T> { _qList.First() };
    }
}

#endregion

#region Eigenstates Implementation

// Eigenstates<T> preserves original input keys using a Dictionary.
// It also supports a projection constructor.
public class Eigenstates<T>
{
    private Dictionary<T, T> _qDict;
    private QuantumStateType _eType = QuantumStateType.Conjunctive;
    private readonly IQuantumOperators<T> _ops;

    // Expose original keys.
    public IReadOnlyCollection<T> States => _qDict.Keys.ToList();

    // Standard constructor: each input maps to itself.
    public Eigenstates(IEnumerable<T> Items, IQuantumOperators<T> ops)
    {
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qDict = Items.ToDictionary(x => x, x => x);
    }
    public Eigenstates(IEnumerable<T> Items) : this(Items, QuantumOperatorsFactory.GetOperators<T>()) { }

    // Projection constructor: apply projection to each input.
    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection, IQuantumOperators<T> ops)
    {
        if (projection == null) throw new ArgumentNullException(nameof(projection));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _qDict = inputValues.ToDictionary(x => x, projection);
    }
    public Eigenstates(IEnumerable<T> inputValues, Func<T, T> projection)
        : this(inputValues, projection, QuantumOperatorsFactory.GetOperators<T>()) { }

    // Internal constructor from a dictionary.
    internal Eigenstates(Dictionary<T, T> dict, IQuantumOperators<T> ops)
    {
        _ops = ops;
        _qDict = dict;
    }

    // Filtering mode setters.
    public Eigenstates<T> Any() { _eType = QuantumStateType.Disjunctive; return this; }
    public Eigenstates<T> All() { _eType = QuantumStateType.Conjunctive; return this; }

    // Arithmetic operations that compute on the stored values but preserve keys.
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        foreach (var kvp in a._qDict)
        {
            foreach (var kvp2 in b._qDict)
            {
                // Preserve the key from a.
                result[kvp.Key] = op(kvp.Value, kvp2.Value);
            }
        }
        return new Eigenstates<T>(result, _ops);
    }

    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, T b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        foreach (var kvp in a._qDict)
        {
            result[kvp.Key] = op(kvp.Value, b);
        }
        return new Eigenstates<T>(result, _ops);
    }

    private Eigenstates<T> Do_oper_type(T a, Eigenstates<T> b, Func<T, T, T> op)
    {
        var result = new Dictionary<T, T>();
        foreach (var kvp in b._qDict)
        {
            result[kvp.Key] = op(a, kvp.Value);
        }
        return new Eigenstates<T>(result, _ops);
    }

    // Arithmetic operator overloads.
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

    // Filtering (comparison) operators.
    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, T value)
    {
        var result = new Dictionary<T, T>();
        foreach (var kvp in _qDict)
        {
            if (condition(kvp.Value, value))
                result[kvp.Key] = kvp.Value;
        }
        return new Eigenstates<T>(result, _ops);
    }
    private Eigenstates<T> Do_condition_type(Func<T, T, bool> condition, Eigenstates<T> other)
    {
        var result = new Dictionary<T, T>();
        foreach (var kvp in _qDict)
        {
            foreach (var kvp2 in other._qDict)
            {
                if (condition(kvp.Value, kvp2.Value))
                {
                    result[kvp.Key] = kvp.Value;
                    break;
                }
            }
        }
        return new Eigenstates<T>(result, _ops);
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

    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();

    // ToValues returns the original keys.
    public IEnumerable<T> ToValues() => _qDict.Keys;
    public override string ToString() =>
        _eType == QuantumStateType.Disjunctive
            ? $"any({string.Join(", ", _qDict.Keys.Distinct())})"
            : $"all({string.Join(", ", _qDict.Keys.Distinct())})";

    // Additional debug method to display keys and projected values.
    public string ToDebugString() =>
        string.Join(", ", _qDict.Select(kvp => $"{kvp.Key} => {kvp.Value}"));
}

#endregion