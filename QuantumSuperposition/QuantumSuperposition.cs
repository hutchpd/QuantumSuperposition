using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

/// <summary>
/// Represents a quantum superposition of states for a given type <typeparamref name="T"/>.
/// Allows performing mathematical and logical operations on these superpositions as if they were in multiple states simultaneously.
/// </summary>
/// <typeparam name="T">The type of elements in the superposition.</typeparam>
public partial class QuBit<T>
{
    public IEnumerable<T> _qList;
    private QuSuper _eType = QuSuper.eConj;

    /// <summary>
    /// Defines the type of superposition: Any, All, or CollapsedResult.
    /// </summary>
    public enum QuSuper
    {
        /// <summary>
        /// Represents a disjunctive superposition (any state can be true).
        /// </summary>
        eConj,
        /// <summary>
        /// Represents a conjunctive superposition (all states must be true).
        /// </summary>
        eDisj,
        /// <summary>
        /// Represents a collapsed superposition (only one state is true).
        /// </summary>
        eCollapsedResult
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuBit{T}"/> class.
    /// </summary>
    /// <param name="Items">The items to include in the superposition.</param>
    /// <param name="type">The type of superposition (Any or All). Default is All.</param>
    public QuBit(IEnumerable<T> Items)
    {
        _qList = Items;

        if (_qList.Distinct().Count() > 1)
            SetType(QuSuper.eDisj); // YOU KNOW IT'S A MULTI-STATE

    }

    /// <summary>
    /// Sets the superposition type to "All" (conjunctive superposition).
    /// </summary>
    /// <returns>The current instance of <see cref="QuantumSuperposition{T}"/>.</returns>
    public bool All(Func<T, bool> predicate)
    {
        return _qList.All(predicate);
    }

    /// <summary>
    /// Sets the superposition type to "Any" (disjunctive superposition).
    /// </summary>
    /// <returns>The current instance of <see cref="QuantumSuperposition{T}"/>.</returns>
    public bool Any(Func<T, bool> predicate)
    {
        return _qList.Any(predicate);
    }

    /// <summary>
    /// Sets the superposition type to "All" (conjunctive superposition).
    /// </summary>
    /// <returns></returns>
    public bool All()
    {
        SetType(QuSuper.eConj);
        return _qList.All(state => !EqualityComparer<T>.Default.Equals(state, default(T)));
    }

    /// <summary>
    /// Sets the superposition type to "Any" (disjunctive superposition).
    /// </summary>
    /// <returns></returns>
    public QuBit<T> Any()
    {
        SetType(QuSuper.eDisj);
        return this;
    }

    // Modulus operators
    /// <summary>
    /// Computes the modulus of a scalar value and a superposition of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The superposition.</param>
    /// <returns>A new superposition resulting from the modulus operation.</returns>
    public static QuBit<T> operator %(T a, QuBit<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_mod);
    }

    /// <summary>
    /// Computes the modulus of two superpositions of states.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the modulus operation.</returns>
    public static QuBit<T> operator %(QuBit<T> a, QuBit<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_mod);
    }

    /// <summary>
    /// Computes the modulus of a superposition of states and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the modulus operation.</returns>
    public static QuBit<T> operator %(QuBit<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_mod);
    }

    // Mathematical operators

    /// <summary>
    /// Adds a scalar value to a superposition of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The superposition.</param>
    /// <returns> A new superposition resulting from the addition operation.</returns>
    public static QuBit<T> operator +(T a, QuBit<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_add);
    }

    /// <summary>
    /// Adds two superpositions of states.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns> A new superposition resulting from the addition operation.</returns>
    public static QuBit<T> operator +(QuBit<T> a, QuBit<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_add);
    }

    /// <summary>
    /// Adds a scalar value to a superposition of states.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value</param>
    /// <returns></returns>
    public static QuBit<T> operator +(QuBit<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_add);
    }

    /// <summary>
    /// Subtracts a scalar value from a superposition of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The superposition.</param>
    /// <returns> A new superposition resulting from the subtraction operation.</returns>
    public static QuBit<T> operator -(T a, QuBit<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_neg);
    }

    /// <summary>
    /// Subtracts two superpositions of states.
    /// </summary>
    /// <param name="b">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns> A new superposition resulting from the subtraction operation.</returns>
    public static QuBit<T> operator -(QuBit<T> a, QuBit<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_neg);
    }

    /// <summary>
    /// Performs subtraction between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the subtraction operation.</returns>
    public static QuBit<T> operator -(QuBit<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_neg);
    }
    /// <summary>
    /// Performs multiplication between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the multiplication operation.</returns>
    public static QuBit<T> operator *(T a, QuBit<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_mult);
    }
    /// <summary>
    /// Performs multiplication between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the multiplication operation.</returns>
    public static QuBit<T> operator *(QuBit<T> a, QuBit<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_mult);
    }

    /// <summary>
    /// Performs multiplication between a superposition and a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuBit<T> operator *(QuBit<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_mult);
    }

    /// <summary>
    /// Performs division between a scalar value and a superposition.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The superposition.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuBit<T> operator /(T a, QuBit<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_div);
    }

    /// <summary>
    /// Performs division between two superpositions.
    /// </summary>
    /// <param name="a">The first superposition.</param>
    /// <param name="b">The second superposition.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuBit<T> operator /(QuBit<T> a, QuBit<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_div);
    }

    /// <summary>
    /// Performs division between a superposition and scalar value 
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new superposition resulting from the division operation.</returns>
    public static QuBit<T> operator /(QuBit<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_div);
    }

    /// <summary>
    /// Determines if any or all states in the superposition are less than a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_lt(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }

    /// <summary>
    /// Determines if any or all states in the superposition are greater than a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_gt(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }
    /// <summary>
    /// Determines if any or all states in the superposition are less than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator <=(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_lteq(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }
    /// <summary>
    /// Determines if any or all states in the superposition are greater than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator >=(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_gteq(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }

    /// <summary>
    /// Determines if any or all states in the superposition are not equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator !=(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_ne(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }

    /// <summary>
    /// Determines if any or all states in the superposition are equal to a scalar value.
    /// </summary>
    /// <param name="a">The superposition.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool operator ==(QuBit<T> a, T b)
    {
        Func<T, bool> ne = x => a.qop_ne(x, b);
        if (Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false))
        {
            return a._qList.All(ne);
        }
        else
        {
            return a._qList.Any(ne);
        }
    }

    /// <summary>
    /// Append an element to the superposition.
    /// </summary>
    /// <param name="element"> The element to append.</param>
    /// <returns> The new instance of <see cref="QuBit{T}"/>.</returns>
    public object Append(T element)
    {
        _qList.Append(element);
        return default;
    }

    private QuBit<T> Do_oper_type(T a, QuBit<T> b, Func<T, T, T> cb)
    {
        var ans = new List<T>();
        foreach (var it in b._qList)
            ans.Add(cb(a, it));
        return new QuBit<T>(ans);
    }

    private QuBit<T> Do_oper_type(QuBit<T> a, T b, Func<T, T, T> cb)
    {
        var ans = new List<T>();
        foreach (var it in a._qList)
            ans.Add(cb(it, b));
        return new QuBit<T>(ans);
    }

    private QuBit<T> Do_oper_type(QuBit<T> a, QuBit<T> b, Func<T, T, T> cb)
    {
        var ans = new List<T>();
        foreach (var it in a._qList)
        {
            foreach (var jt in b._qList)
                ans.Add(cb(it, jt));
        }
        return new QuBit<T>(ans);
    }

    // Might need this for the logical operators
    private static Func<T, T, TResult> CreateBinaryOperation<TResult>(ExpressionType op)
    {
        if (IsDynamicType())
        {
            return (a, b) =>
            {
                dynamic da = a, db = b;
                return (TResult)(op switch
                {
                    ExpressionType.Add => da + db,
                    ExpressionType.Multiply => da * db,
                    ExpressionType.Subtract => da - db,
                    ExpressionType.Divide => da / db,
                    ExpressionType.Modulo => da % db,
                    ExpressionType.Equal => da == db,
                    ExpressionType.NotEqual => da != db,
                    ExpressionType.GreaterThan => da > db,
                    ExpressionType.LessThan => da < db,
                    ExpressionType.GreaterThanOrEqual => da >= db,
                    ExpressionType.LessThanOrEqual => da <= db,
                    _ => throw new NotSupportedException($"Operation '{op}' not supported")
                });
            };
        }

        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.MakeBinary(op, paramA, paramB);
        return Expression.Lambda<Func<T, T, TResult>>(body, paramA, paramB).Compile();
    }

    private static bool IsDynamicType()
    {
        return typeof(T) == typeof(object); // Best we can do at runtime in C#
    }


    /// <summary>
    /// Method to compute the modulus of two values.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>Result of the modulo operation</returns>
    public T qop_mod(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Modulo(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Method to add two values.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>Result of addition.</returns>
    public T qop_add(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Add(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }

    /// <summary>
    /// Method to negate a value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value (for symmetry in function signatures).</param>
    /// <returns>Negated value.</returns>
    public T qop_neg(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Add(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Method to multiply two values.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>Result of multiplication.</returns>
    public T qop_mult(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Multiply(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Method to divide two values.
    /// </summary>
    /// <param name="a">Dividend.</param>
    /// <param name="b">Divisor.</param>
    /// <returns>Result of division.</returns>
    public T qop_div(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Divide(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Checks if the first value is greater than the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if first value is greater; otherwise, false.</returns>
    public bool qop_gt(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.GreaterThan(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Checks if the first value is greater than or equal to the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if first value is greater or equal; otherwise, false.</returns>
    public bool qop_gteq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.GreaterThanOrEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Checks if the first value is less than the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if first value is less; otherwise, false.</returns>
    public bool qop_lt(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.LessThan(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Checks if the first value is less than or equal to the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if first value is less or equal; otherwise, false.</returns>
    public bool qop_lteq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.LessThanOrEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Checks if the first value is not equal to the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if values are not equal; otherwise, false.</returns>
    public bool qop_ne(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.NotEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Checks if the first value is equal to the second value.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True if values are equal; otherwise, false.</returns>
    public bool qop_eq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Equal(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Gets the current type of the eigenstates (conjunctive, disjunctive, or collapsed).
    /// </summary>
    /// <returns>The current type of the eigenstates.</returns>
    public object GetCurrentType()
    {
        return _eType;
    }
    /// <summary>
    /// Sets the type of the eigenstates (conjunctive, disjunctive, or collapsed).
    /// </summary>
    /// <param name="t">The type to set.</param>
    private void SetType(QuSuper t)
    {
        _eType = t;
    }
    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        // Check if the object is a reference to itself
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // Check if the object is null or not of the same type
        if (obj is not QuBit<T> other)
        {
            return false;
        }

        // Check if both objects have the same type and contain the same elements
        return _eType == other._eType && _qList.SequenceEqual(other._qList);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        // Combine the hash codes of the type and the list elements
        int hash = _eType.GetHashCode();
        foreach (var item in _qList)
        {
            hash = HashCode.Combine(hash, item?.GetHashCode() ?? 0);
        }

        return hash;
    }

    /// <summary>
    /// Method to get the values from the quantum superposition.
    /// </summary>
    /// <returns>Values from the quantum superposition.</returns>
    /// <exception cref="InvalidOperationException"> Thrown when there are no values to collapse.</exception>
    public IEnumerable<T> ToValues()
    {
        // If no values, throw an exception
        if (!_qList.Any())
            throw new InvalidOperationException("No values to collapse.");

        // Return all values if in disjunctive superposition
        if (_eType == QuSuper.eDisj)
        {
            return _qList;
        }
        else if (_eType == QuSuper.eConj)
        {
            // If only one distinct value remains, return it as a single item list
            if (_qList.Distinct().Count() == 1)
                return new List<T> { _qList.First() };

            // Otherwise, return all the values
            return _qList;
        }
        else
        {
            // If in the collapsed state or any other state that means a single value
            return new List<T> { _qList.First() };
        }
    }

    public override string ToString()
    {
        // Always use the internal _eType to decide on output.
        // If in disjunctive mode (eDisj), always print in the any(...) format,
        // even if _qList.Distinct() returns just one element.
        if (_eType == QuSuper.eDisj)
        {
            // Use the distinct list, but force the "any(...)" format.
            var distinctVals = _qList.Distinct().ToList();
            return $"any({string.Join(", ", distinctVals)})";
        }
        else
        {
            // Otherwise, if in conjunctive mode, print normally:
            var distinctVals = _qList.Distinct().ToList();
            if (distinctVals.Count == 1)
                return distinctVals[0].ToString();
            return $"all({string.Join(", ", distinctVals)})";
        }
    }

}

/// <summary>
/// Represents a collection of eigenstates for a given type <typeparamref name="T"/>.
/// Supports various mathematical and logical operations on these eigenstates.
/// </summary>
/// <typeparam name="T">The type of elements in the eigenstates.</typeparam>
public partial class Eigenstates<T>
{
    private new Dictionary<T, T> _qList;
    private QuSuper _eType = QuSuper.eConj;
    /// <summary>
    /// 
    /// </summary>
    private enum QuSuper
    {
        /// <summary>
        /// Represents a conjunctive eigenstate (all states must satisfy a condition).
        /// </summary>
        eConj,
        /// <summary>
        /// Represents a disjunctive eigenstate (any state can satisfy a condition).
        /// </summary>
        eDisj,
        /// <summary>
        /// Represents a collapsed eigenstate (only one state is considered).
        /// </summary>
        eCollapsedResult
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Eigenstates{T}"/> class from a collection of items.
    /// </summary>
    /// <param name="Items">The items to include in the eigenstates.</param>
    public Eigenstates(IEnumerable<T> Items)
    {
        _qList = Items.ToDictionary(x => x, x => x);
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Eigenstates{T}"/> class from a dictionary of items.
    /// </summary>
    /// <param name="Items">The dictionary of items to include in the eigenstates.</param>
    public Eigenstates(Dictionary<T, T> Items)
    {
        _qList = Items;
    }
    /// <summary>
    /// Determines if a scalar value is not equal to any state in the eigenstates.
    /// </summary>
    /// <param name="a">The scalar value to compare.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator !=(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_ne);
    }
    /// <summary>
    /// Determines if a scalar value is equal to any state in the eigenstates.
    /// </summary>
    /// <param name="a">The scalar value to compare.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator ==(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_eq);
    }
    /// <summary>
    /// Determines if a scalar value is less than any state in the eigenstates.
    /// </summary>
    /// <param name="a">The scalar value to compare.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator <(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_lt);
    }
    /// <summary>
    /// Determines if a scalar value is greater than any state in the eigenstates.
    /// </summary>
    /// <param name="a">The scalar value to compare.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_gt);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Eigenstates<T> operator <=(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_lteq);
    }
    /// <summary>
    /// Determines if a scalar value is greater than than or equal to any state in the eigenstates.
    /// </summary>
    /// <param name="a">The scalar value to compare.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >=(T a, Eigenstates<T> b)
    {
        return b.Do_condition_type(a, b, b.qop_gteq);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is not equal to any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator !=(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_ne);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is equal to any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator ==(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_eq);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is less than any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator <(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_lt);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is greater than any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_gt);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is less than or equal to any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator <=(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_lteq);
    }
    /// <summary>
    /// Determines if any state in the first eigenstates is greater than or equal to any state in the second eigenstates.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >=(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_condition_type(a, b, a.qop_gteq);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is not equal to a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator !=(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_ne);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is equal to a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator ==(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_eq);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is less than a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator <(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_lt);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is greater than a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_gt);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is less than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator <=(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_lteq);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is greater than or equal to a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator >=(Eigenstates<T> a, T b)
    {
        return a.Do_condition_type(a, b, a.qop_gteq);
    }
    /// <summary>
    /// Computes the modulus of a scalar value and an eigenstates of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates.</param>
    /// <returns>A new eigenstates resulting from the modulus operation.</returns>
    public static Eigenstates<T> operator %(T a, Eigenstates<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_mod);
    }
    /// <summary>
    /// Computes the modulus of an eigenstates of states and an eigenstates of states.
    /// </summary>
    /// <param name="a">The first eigenstate of states</param>
    /// <param name="b">The second eigenstate of states.</param>
    /// <returns>A new eigenstates resulting from the modulus operation.</returns>
    public static Eigenstates<T> operator %(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_mod);
    }
    /// <summary>
    /// Determines if any state in the eigenstates is less than a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result.</returns>
    public static Eigenstates<T> operator %(Eigenstates<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_mod);
    }
    /// <summary>
    /// Adds a scalar value to an eigenstates of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates.</param>
    /// <returns>A new eigenstates resulting from the addition operation.</returns>
    public static Eigenstates<T> operator +(T a, Eigenstates<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_add);
    }

    /// <summary>
    /// Adds two eigenstates of states.
    /// </summary>
    /// <param name="a">The first eigenstates.</param>
    /// <param name="b">The second eigenstates.</param>
    /// <returns>A new eigenstates resulting from the addition operation.</returns>
    public static Eigenstates<T> operator +(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_add);
    }

    /// <summary>
    /// Adds an eigenstates of states and a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new eigenstates resulting from the addition operation.</returns>
    public static Eigenstates<T> operator +(Eigenstates<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_add);
    }
    /// <summary>
    /// Subtracts a scalar value from an eigenstates of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates.</param>
    /// <returns>A new eigenstates resulting from the subtraction operation.</returns>
    public static Eigenstates<T> operator -(T a, Eigenstates<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_neg);
    }

    /// <summary>
    /// Subtracts two eigenstates of states.
    /// </summary>
    /// <param name="a">The first eigenstates.</param>
    /// <param name="b">The second eigenstates.</param>
    /// <returns>A new eigenstates resulting from the subtraction operation.</returns>
    public static Eigenstates<T> operator -(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_neg);
    }

    /// <summary>
    /// Subtracts a scalar value from an eigenstates of states.
    /// </summary>
    /// <param name="a">The eigenstates.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new eigenstates resulting from the subtraction operation.</returns>
    public static Eigenstates<T> operator -(Eigenstates<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_neg);
    }

    /// <summary>
    /// Multiplies a scalar value with an eigenstates of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates.</param>
    /// <returns>A new eigenstates resulting from the multiplication operation.</returns>
    public static Eigenstates<T> operator *(T a, Eigenstates<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_mult);
    }

    /// <summary>
    /// Multiplies two eigenstates of states.
    /// </summary>
    /// <param name="a">The first eigenstates.</param>
    /// <param name="b">The second eigenstates.</param>
    /// <returns>A new eigenstates resulting from the multiplication operation.</returns>
    public static Eigenstates<T> operator *(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_mult);
    }

    /// <summary>
    /// Multiplies an eigenstates of states with a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new eigenstates resulting from the multiplication operation.</returns>
    public static Eigenstates<T> operator *(Eigenstates<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_mult);
    }

    /// <summary>
    /// Divides a scalar value by an eigenstates of states.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates.</param>
    /// <returns>A new eigenstates resulting from the division operation.</returns>
    public static Eigenstates<T> operator /(T a, Eigenstates<T> b)
    {
        return b.Do_oper_type(a, b, b.qop_div);
    }

    /// <summary>
    /// Divides one eigenstates of states by another eigenstates of states.
    /// </summary>
    /// <param name="a">The first eigenstates.</param>
    /// <param name="b">The second eigenstates.</param>
    /// <returns>A new eigenstates resulting from the division operation.</returns>
    public static Eigenstates<T> operator /(Eigenstates<T> a, Eigenstates<T> b)
    {
        return a.Do_oper_type(a, b, b.qop_div);
    }

    /// <summary>
    /// Divides an eigenstates of states by a scalar value.
    /// </summary>
    /// <param name="a">The eigenstates.</param>
    /// <param name="b">The scalar value.</param>
    /// <returns>A new eigenstates resulting from the division operation.</returns>
    public static Eigenstates<T> operator /(Eigenstates<T> a, T b)
    {
        return a.Do_oper_type(a, b, a.qop_div);
    }

    /// <summary>
    /// Computes the modulus of two values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The result of the modulo operation.</returns>
    private T qop_mod(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Modulo(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }

    /// <summary>
    /// Adds two values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The result of the addition operation.</returns>
    private T qop_add(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Add(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }

    /// <summary>
    /// Negates a value by performing an addition operation (note: symmetric to subtraction in some contexts).
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value (used for symmetry).</param>
    /// <returns>The result of the negation operation.</returns>
    private T qop_neg(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Add(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Multiplies two values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The result of the multiplication operation.</returns>
    private T qop_mult(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Multiply(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Divides two values.
    /// </summary>
    /// <param name="a">The first value (dividend).</param>
    /// <param name="b">The second value (divisor).</param>
    /// <returns>The result of the division operation.</returns>
    private T qop_div(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Divide(paramA, paramB);
        var Modu = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
        return Modu(a, b);
    }
    /// <summary>
    /// Determines if the first value is greater than the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the first value is greater than the second; otherwise, false.</returns>
    private bool qop_gt(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.GreaterThan(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Determines if the first value is greater than or equal to the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the first value is greater than or equal to the second; otherwise, false.</returns>
    private bool qop_gteq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.GreaterThanOrEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Determines if the first value is less than the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the first value is less than the second; otherwise, false.</returns>
    private bool qop_lt(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.LessThan(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Determines if the first value is less than or equal to the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the first value is less than or equal to the second; otherwise, false.</returns>
    private bool qop_lteq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.LessThanOrEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Determines if the first value is not equal to the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the values are not equal; otherwise, false.</returns>
    private bool qop_ne(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.NotEqual(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Determines if the first value is equal to the second value.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>True if the values are equal; otherwise, false.</returns>
    private bool qop_eq(T a, T b)
    {
        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        var body = Expression.Equal(paramA, paramB);
        var Equal = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
        return Equal(a, b);
    }
    /// <summary>
    /// Performs a conditional operation between a scalar value and an eigenstates, using a specified condition.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates to compare against.</param>
    /// <param name="cb">The condition delegate to use for comparison.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the condition.</returns>
    private Eigenstates<T> Do_condition_type(T a, Eigenstates<T> b, Func<T, T, bool> cb)
    {
        var ans = new Dictionary<T, T>();
        if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(b.GetCurrentType(), QuSuper.eConj, false)))
        {
        }
        // only true if a is great than all.. and then what.
        else
        {
            foreach (var it in b._qList)
            {
                if (cb(a, it.Value))
                    ((IDictionary<T, T>)ans).Add(it);
            }
        }

        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Performs a conditional operation between an eigenstates and a scalar value, using a specified condition.
    /// </summary>
    /// <param name="a">The eigenstates to compare.</param>
    /// <param name="b">The scalar value to compare against.</param>
    /// <param name="cb">The condition delegate to use for comparison.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the condition.</returns>
    private Eigenstates<T> Do_condition_type(Eigenstates<T> a, T b, Func<T, T, bool> cb)
    {
        var ans = new Dictionary<T, T>();
        foreach (var it in a._qList)
        {
            if (cb(b, it.Value))
                ((IDictionary<T, T>)ans).Add(it);
        }
        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Performs a conditional operation between two eigenstates, using a specified condition.
    /// </summary>
    /// <param name="a">The first eigenstates to compare.</param>
    /// <param name="b">The second eigenstates to compare against.</param>
    /// <param name="cb">The condition delegate to use for comparison.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the condition.</returns>
    private Eigenstates<T> Do_condition_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, bool> cb)
    {
        var ans = new Dictionary<T, T>();

        if (Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eConj, false), Operators.ConditionalCompareObjectEqual(b.GetCurrentType(), QuSuper.eDisj, false))))
        {
            // give me all b's that are bigger than every a.. same as the other so we can just switch the deligates.
            var c = a;
            a = b;
            b = c;
        }
        
        foreach (var it in a._qList)
        {
            if (Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eDisj, false), Operators.ConditionalCompareObjectEqual(b.GetCurrentType(), QuSuper.eDisj, false))))
            {
                foreach (var jt in b._qList)
                {
                    // give me all a's that are bigger than any b's e.g.
                    if (!ans.ContainsKey(it.Key))
                    {
                        // why calculate it if we have the answer already?
                        if (cb(it.Value, jt.Value))
                        {
                            ((IDictionary<T, T>)ans).Add(it);
                        }
                    }
                }
            }
            else if (Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectEqual(a.GetCurrentType(), QuSuper.eDisj, false), Operators.ConditionalCompareObjectEqual(b.GetCurrentType(), QuSuper.eConj, false))))
            {
                // Give me every a that is bigger than every b e.g.
                if (!ans.ContainsKey(it.Key))
                {
                    var Bvalues = b._qList.Values;
                    Func<T, bool> ca = x => cb(it.Value, x);
                    if (Bvalues.All(ca))
                    {
                        ((IDictionary<T, T>)ans).Add(it);
                    }
                }
            }
        }

        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Performs an operation between a scalar value and an eigenstates, using a specified operation.
    /// </summary>
    /// <param name="a">The scalar value.</param>
    /// <param name="b">The eigenstates to operate on.</param>
    /// <param name="cb">The operation delegate to use.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the operation.</returns>
    private Eigenstates<T> Do_oper_type(T a, Eigenstates<T> b, Func<T, T, T> cb)
    {
        var ans = new Dictionary<T, T>();
        foreach (var it in b._qList)
            ((IDictionary<T, T>)ans).Add(new KeyValuePair<T, T>(it.Key, cb(a, it.Value)));
        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Performs an operation between an eigenstates and a scalar value, using a specified operation.
    /// </summary>
    /// <param name="a">The eigenstates to operate on.</param>
    /// <param name="b">The scalar value to operate with.</param>
    /// <param name="cb">The operation delegate to use.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the operation.</returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, T b, Func<T, T, T> cb)
    {
        var ans = new Dictionary<T, T>();
        foreach (var it in a._qList)
            ((IDictionary<T, T>)ans).Add(new KeyValuePair<T, T>(it.Key, cb(it.Value, b)));
        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Performs an operation between two eigenstates, using a specified operation.
    /// </summary>
    /// <param name="a">The first eigenstates to operate on.</param>
    /// <param name="b">The second eigenstates to operate with.</param>
    /// <param name="cb">The operation delegate to use.</param>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> containing the result of the operation.</returns>
    private Eigenstates<T> Do_oper_type(Eigenstates<T> a, Eigenstates<T> b, Func<T, T, T> cb)
    {
        var ans = new Dictionary<T, T>();
        foreach (var it in a._qList)
        {
            foreach (var jt in b._qList)
                ((IDictionary<T, T>)ans).Add(new KeyValuePair<T, T>(it.Key, cb(it.Value, jt.Value)));
        }
        return new Eigenstates<T>(ans);
    }
    /// <summary>
    /// Converts the current eigenstates into a string representation.
    /// </summary>
    /// <returns>A string representing the keys of the eigenstates.</returns>
    public override string ToString()
    {
        if (_eType == QuSuper.eDisj)
            return $"any({string.Join(", ", _qList.Distinct())})";

        var vals = _qList.Distinct().ToList();
        if (vals.Count == 1)
            return vals[0].ToString();

        return $"any({string.Join(", ", vals)})";
    }

    /// <summary>
    /// Sets the type of the eigenstates to conjunctive (All).
    /// </summary>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> with the type set to conjunctive.</returns>
    public Eigenstates<T> All()
    {
        var b = new Eigenstates<T>(_qList);
        b.SetType(QuSuper.eConj);
        return b;
    }
    /// <summary>
    /// Sets the type of the eigenstates to disjunctive (Any).
    /// </summary>
    /// <returns>A new instance of <see cref="Eigenstates{T}"/> with the type set to disjunctive.</returns>
    public Eigenstates<T> Any()
    {
        var b = new Eigenstates<T>(_qList);
        b.SetType(QuSuper.eDisj);
        return b;
    }
    /// <summary>
    /// Gets the current type of the eigenstates (conjunctive, disjunctive, or collapsed).
    /// </summary>
    /// <returns>The current type of the eigenstates.</returns>
    public object GetCurrentType()
    {
        return _eType;
    }

    /// <summary>
    /// Sets the type of the eigenstates.
    /// </summary>
    /// <param name="t">The type to set (conjunctive, disjunctive, or collapsed).</param>
    private void SetType(QuSuper t)
    {
        _eType = t;
    }

    /// <summary>
    /// Collapses the eigenstates to a single value or a list of values.
    /// </summary>
    /// <returns>An IEnumerable of the values of the eigenstate or a single value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there are no values to collapse.</exception>
    public IEnumerable<T> ToValues()
    {
        // If no values, throw an exception
        if (_qList.Count == 0)
            throw new InvalidOperationException("No values to collapse.");

        // Return all values if in a certain state (like a list context in Perl)
        if (_eType == QuSuper.eDisj)
        {
            return _qList.Values;
        }
        else if (_eType == QuSuper.eConj)
        {
            // If only one distinct value remains, return it as a single item list
            if (_qList.Values.Distinct().Count() == 1)
                return new List<T> { _qList.Values.First() };

            // Otherwise, return all the values
            return _qList.Values;
        }
        else
        {
            // If in the collapsed state or any other state that means a single value
            return new List<T> { _qList.Values.First() };
        }
    }


}