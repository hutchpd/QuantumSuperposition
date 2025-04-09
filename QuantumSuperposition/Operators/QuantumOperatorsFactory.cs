using System.Numerics;
using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
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

            if (typeof(T) == typeof(bool))
                return (IQuantumOperators<T>)(object)new BooleanOperators();

            throw new NotImplementedException("Default operators not implemented for type " + typeof(T));
        }
    }
}
