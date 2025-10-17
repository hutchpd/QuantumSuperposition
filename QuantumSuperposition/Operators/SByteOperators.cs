using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Like Byte, but occasionally broods in the shadows of negativity.
    /// </summary>
    public class SByteOperators : IQuantumOperators<sbyte>
    {
        public sbyte Add(sbyte a, sbyte b)
        {
            return (sbyte)(a + b);
        }

        public sbyte Subtract(sbyte a, sbyte b)
        {
            return (sbyte)(a - b);
        }

        public sbyte Multiply(sbyte a, sbyte b)
        {
            return (sbyte)(a * b);
        }

        public sbyte Divide(sbyte a, sbyte b)
        {
            return (sbyte)(a / b);
        }

        public sbyte Mod(sbyte a, sbyte b)
        {
            return (sbyte)(a % b);
        }

        public bool GreaterThan(sbyte a, sbyte b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(sbyte a, sbyte b)
        {
            return a >= b;
        }

        public bool LessThan(sbyte a, sbyte b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(sbyte a, sbyte b)
        {
            return a <= b;
        }

        public bool Equal(sbyte a, sbyte b)
        {
            return a == b;
        }

        public bool NotEqual(sbyte a, sbyte b)
        {
            return a != b;
        }

        public bool IsAddCommutative => true;
    }
}
