namespace QuantumSuperposition.Core
{
    /// <summary>
    /// A set of mathematical operations tailored for types that wish they were numbers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        bool IsAddCommutative { get; }
    }

}
