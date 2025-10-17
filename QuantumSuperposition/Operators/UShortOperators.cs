using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Unsigned and proud. Short, but always positive.
    /// </summary>
    public class UShortOperators : IQuantumOperators<ushort>
    {
        public ushort Add(ushort a, ushort b)
        {
            return (ushort)(a + b);
        }

        public ushort Subtract(ushort a, ushort b)
        {
            return (ushort)(a - b);
        }

        public ushort Multiply(ushort a, ushort b)
        {
            return (ushort)(a * b);
        }

        public ushort Divide(ushort a, ushort b)
        {
            return (ushort)(a / b);
        }

        public ushort Mod(ushort a, ushort b)
        {
            return (ushort)(a % b);
        }

        public bool GreaterThan(ushort a, ushort b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(ushort a, ushort b)
        {
            return a >= b;
        }

        public bool LessThan(ushort a, ushort b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(ushort a, ushort b)
        {
            return a <= b;
        }

        public bool Equal(ushort a, ushort b)
        {
            return a == b;
        }

        public bool NotEqual(ushort a, ushort b)
        {
            return a != b;
        }

        public bool IsAddCommutative => true;
    }
}
