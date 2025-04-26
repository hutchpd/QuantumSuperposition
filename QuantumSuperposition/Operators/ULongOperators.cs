using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Thinks long thoughts, never says a negative word.
    /// </summary>
    public class ULongOperators : IQuantumOperators<ulong>
    {
        public ulong Add(ulong a, ulong b) => a + b;
        public ulong Subtract(ulong a, ulong b) => a - b;
        public ulong Multiply(ulong a, ulong b) => a * b;
        public ulong Divide(ulong a, ulong b) => a / b;
        public ulong Mod(ulong a, ulong b) => a % b;

        public bool GreaterThan(ulong a, ulong b) => a > b;
        public bool GreaterThanOrEqual(ulong a, ulong b) => a >= b;
        public bool LessThan(ulong a, ulong b) => a < b;
        public bool LessThanOrEqual(ulong a, ulong b) => a <= b;
        public bool Equal(ulong a, ulong b) => a == b;
        public bool NotEqual(ulong a, ulong b) => a != b;

        public bool IsAddCommutative => true;
    }
}
