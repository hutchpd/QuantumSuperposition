using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// Represents a quantum superposition of states for a given type <typeparamref name="T"/>.
/// Allows performing mathematical and logical operations on these superpositions as if they were in multiple states simultaneously.
/// </summary>
/// <typeparam name="T">The type of elements in the superposition.</typeparam>
public class QuantumSuperposition<T>
{
    private List<T> _states;
    private SuperpositionType _type;

    /// <summary>
    /// Defines the type of superposition: Any, All, or CollapsedResult.
    /// </summary>
    public enum SuperpositionType
    {
        /// <summary>
        /// Represents a disjunctive superposition (any state can be true).
        /// </summary>
        Any,
        /// <summary>
        /// Represents a conjunctive superposition (all states must be true).
        /// </summary>
        All,
        /// <summary>
        /// Represents a collapsed result after an operation.
        /// </summary>
        CollapsedResult
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuantumSuperposition{T}"/> class.
    /// </summary>
    /// <param name="states">The list of states to be included in the superposition.</param>
    /// <param name="type">The type of superposition (Any or All). Default is All.</param>
    public QuantumSuperposition(IEnumerable<T> states, SuperpositionType type = SuperpositionType.All)
    {
        _states = states.ToList();
        _type = type;
    }

    /// <summary>
    /// Sets the superposition type to "All" (conjunctive superposition).
    /// </summary>
    /// <returns>The current instance of <see cref="QuantumSuperposition{T}"/>.</returns>
    public QuantumSuperposition<T> All()
    {
        _type = SuperpositionType.All;
        return this;
    }

    /// <summary>
    /// Sets the superposition type to "Any" (disjunctive superposition).
    /// </summary>
    /// <returns>The current instance of <see cref="QuantumSuperposition{T}"/>.</returns>
    public QuantumSuperposition<T> Any()
    {
        _type = SuperpositionType.Any;
        return this;
    }

    /// <summary>
    /// Returns the states (eigenstates) in the superposition that match a given condition.
    /// </summary>
    /// <param name="condition">The condition to filter states.</param>
    /// <returns>An enumerable collection of states that satisfy the condition.</returns>
    public IEnumerable<T> EigenstatesWhere(Func<T, bool> condition)
    {
        return _states.Where(condition);
    }

    // Modulus operators
    /// <summary>
    /// Computes the modulus of a scalar value and a superposition of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The superposition.</param>
    /// <returns>A new superposition resulting from the modulus operation.</returns>
    public static QuantumSuperposition<T> operator %(T a, QuantumSuperposition<T> b) => b.DoOperation(a, b.Modulo);

    /// <summary>
    /// Computes the modulus of a superposition of states and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the modulus operation.</returns>
    public static QuantumSuperposition<T> operator %(QuantumSuperposition<T> a, T b) => a.DoOperation(b, a.Modulo);

    /// <summary>
    /// Computes the modulus of two superpositions of states.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns></returns>
    public static QuantumSuperposition<T> operator %(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.DoOperation(b, a.Modulo);

    /// <summary>
    /// Generates a function that computes the modulus of two values.
    /// </summary>
    private Func<T, T, T> Modulo = GenerateBinaryOperation(Expression.Modulo);

    // Mathematical operators
    /// <summary>
    /// Adds a scalar value to a superposition of states.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the addition operation.</returns>
    public static QuantumSuperposition<T> operator +(QuantumSuperposition<T> a, T b) => a.DoOperation(b, a.Add);

    /// <summary>
    /// Performs addition between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the addition operation.</returns>
    public static QuantumSuperposition<T> operator +(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.DoOperation(b, a.Add);

    /// <summary>
    /// Performs subtraction between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the subtraction operation.</returns>
    public static QuantumSuperposition<T> operator -(QuantumSuperposition<T> a, T b) => a.DoOperation(b, a.Subtract);

    /// <summary>
    /// Performs subtraction between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the subtraction operation.</returns>
    public static QuantumSuperposition<T> operator -(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.DoOperation(b, a.Subtract);

    /// <summary>
    /// Performs multiplication between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the multiplication operation.</returns>
    public static QuantumSuperposition<T> operator *(QuantumSuperposition<T> a, T b) => a.DoOperation(b, a.Multiply);

    /// <summary>
    /// Performs multiplication between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the multiplication operation.</returns>
    public static QuantumSuperposition<T> operator *(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.DoOperation(b, a.Multiply);

    /// <summary>
    /// Performs division between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuantumSuperposition<T> operator /(QuantumSuperposition<T> a, T b) => a.DoOperation(b, a.Divide);

    /// <summary>
    /// Performs division between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuantumSuperposition<T> operator /(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.DoOperation(b, a.Divide);

    // Comparison operators for scalar comparisons
    /// <summary>
    /// Determines if any or all states in the superposition are equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator ==(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.Equals);

    /// <summary>
    /// Determines if any or all states in the superposition are not equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator !=(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.NotEquals);

    /// <summary>
    /// Determines if any or all states in the superposition are less than a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.LessThan);

    /// <summary>
    /// Determines if any or all states in the superposition are greater than a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.GreaterThan);

    /// <summary>
    /// Determines if any or all states in the superposition are less than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <=(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.LessThanOrEqual);

    /// <summary>
    /// Determines if any or all states in the superposition are greater than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >=(QuantumSuperposition<T> a, T b) => a.CompareAllOrAnyWithScalar(b, a.GreaterThanOrEqual);

    // Overloaded Comparison operators for QuantumSuperposition<T> to QuantumSuperposition<T>
    /// <summary>
    /// Determines if any or all states in the first superposition are less than those in the second superposition.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.CompareAllOrAnyWithSuperposition(b, a.LessThan);

    /// <summary>
    /// Determines if any or all states in the first superposition are greater than those in the second superposition.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.CompareAllOrAnyWithSuperposition(b, a.GreaterThan);

    /// <summary>
    /// Determines if any or all states in the first superposition are less than or equal to those in the second superposition.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <=(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.CompareAllOrAnyWithSuperposition(b, a.LessThanOrEqual);

    /// <summary>
    /// Determines if any or all states in the first superposition are greater than or equal to those in the second superposition.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >=(QuantumSuperposition<T> a, QuantumSuperposition<T> b) => a.CompareAllOrAnyWithSuperposition(b, a.GreaterThanOrEqual);

    /// <summary>
    /// Performs a specified operation on the superposition with a scalar value.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <param name="operation">The operation to perform.</param>
    /// <returns>A new superposition resulting from the operation.</returns>
    private QuantumSuperposition<T> DoOperation(T value, Func<T, T, T> operation)
    {
        var results = _states.Select(state => operation(state, value)).ToList();
        return new QuantumSuperposition<T>(results, _type);
    }

    /// <summary>
    /// Performs a specified operation on the superposition with a superposition.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    private QuantumSuperposition<T> DoOperation(QuantumSuperposition<T> other, Func<T, T, T> operation)
    {
        var results = new List<T>();
        foreach (var stateA in _states)
        {
            foreach (var stateB in other._states)
            {
                results.Add(operation(stateA, stateB));
            }
        }
        return new QuantumSuperposition<T>(results, _type);
    }

    /// <summary>
    /// Determines if any or all states in the superposition are equal to a scalar value.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <param name="comparison">The comparison function.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    private bool CompareAllOrAnyWithScalar(T value, Func<T, T, bool> comparison)
    {
        if (_type == SuperpositionType.All)
        {
            return _states.All(state => comparison(state, value));
        }
        else
        {
            return _states.Any(state => comparison(state, value));
        }
    }

    /// <summary>
    /// Determines if any or all states in the superposition are compared with another superposition using a specified comparison function.
    /// </summary>
    /// <param name="other">The other superposition to compare with.</param>
    /// <param name="comparison">The comparison function.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    private bool CompareAllOrAnyWithSuperposition(QuantumSuperposition<T> other, Func<T, T, bool> comparison)
    {
        if (_type == SuperpositionType.All && other._type == SuperpositionType.All)
        {
            // All states from 'this' should be compared with all states from 'other'
            return _states.All(stateA => other._states.All(stateB => comparison(stateA, stateB)));
        }
        else if (_type == SuperpositionType.Any || other._type == SuperpositionType.Any)
        {
            // Any state from 'this' should be compared with any state from 'other'
            return _states.Any(stateA => other._states.Any(stateB => comparison(stateA, stateB)));
        }
        else
        {
            // Default comparison when types are mixed
            return _states.Any(stateA => other._states.All(stateB => comparison(stateA, stateB)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private Func<T, T, T> Add = GenerateBinaryOperation(Expression.Add);
    /// <summary>
    /// 
    /// </summary>
    private Func<T, T, T> Subtract = GenerateBinaryOperation(Expression.Subtract);
    /// <summary>
    /// 
    /// </summary>
    private Func<T, T, T> Multiply = GenerateBinaryOperation(Expression.Multiply);
    /// <summary>
    /// 
    /// </summary>
    private Func<T, T, T> Divide = GenerateBinaryOperation(Expression.Divide);

    // Example using Expressions for dynamic operation generation
    /// <summary>
    /// Generates a binary operation function using the specified operation expression.
    /// </summary>
    /// <param name="operation">The operation expression.</param>
    /// <returns>A binary operation function.</returns>
    private static Func<T, T, T> GenerateBinaryOperation(Func<Expression, Expression, BinaryExpression> operation)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = operation(paramA, paramB);
        return Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
    }

    // Comparison operations
    /// <summary>
    /// Determines whether two specified instances of <typeparamref name="T"/> are equal.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if the two instances are equal; otherwise, false.</returns>
    private bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
    /// <summary>
    /// Determines whether two specified instances of <typeparamref name="T"/> are not equal.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if the two instances are not equal; otherwise, false.</returns>
    private bool NotEquals(T a, T b) => !EqualityComparer<T>.Default.Equals(a, b);
    /// <summary>
    /// Determines whether the first specified instance of <typeparamref name="T"/> is less than the second specified instance.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, false.</returns>
    private bool LessThan(T a, T b) => Comparer<T>.Default.Compare(a, b) < 0;
    /// <summary>
    /// Determines whether the first specified instance of <typeparamref name="T"/> is greater than the second specified instance.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, false.</returns>
    private bool GreaterThan(T a, T b) => Comparer<T>.Default.Compare(a, b) > 0;
    /// <summary>
    /// Determines whether the first specified instance of <typeparamref name="T"/> is less than or equal to the second specified instance.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, false.</returns>
    private bool LessThanOrEqual(T a, T b) => Comparer<T>.Default.Compare(a, b) <= 0;
    /// <summary>
    /// Determines whether the first specified instance of <typeparamref name="T"/> is greater than or equal to the second specified instance.
    /// </summary>
    /// <param name="a">The first instance to compare.</param>
    /// <param name="b">The second instance to compare.</param>
    /// <returns>true if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, false.</returns>
    private bool GreaterThanOrEqual(T a, T b) => Comparer<T>.Default.Compare(a, b) >= 0;

    /// <summary>
    /// Determines whether the specified object is equal to the current instance of <see cref="QuantumSuperposition{T}"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is QuantumSuperposition<T> other)
        {
            return _states.SequenceEqual(other._states);
        }
        return false;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => _states.GetHashCode();

    /// <summary>
    /// Returns a string representation of the current superposition and its type.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"{_type}({string.Join(", ", _states)})";
}
