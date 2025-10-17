using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Short, fast, and to the point. No tall tales here.
    /// </summary>
    public class ShortOperators : IQuantumOperators<short>
    {
        public short Add(short a, short b)
        {
            return (short)(a + b);
        }

        public short Subtract(short a, short b)
        {
            return (short)(a - b);
        }

        public short Multiply(short a, short b)
        {
            return (short)(a * b);
        }

        public short Divide(short a, short b)
        {
            return (short)(a / b);
        }

        public short Mod(short a, short b)
        {
            return (short)(a % b);
        }

        public bool GreaterThan(short a, short b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(short a, short b)
        {
            return a >= b;
        }

        public bool LessThan(short a, short b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(short a, short b)
        {
            return a <= b;
        }

        public bool Equal(short a, short b)
        {
            return a == b;
        }

        public bool NotEqual(short a, short b)
        {
            return a != b;
        }

        public bool IsAddCommutative => true;
    }
}
