using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// The long-hauler. Bigger numbers, bigger dreams.
    /// </summary>
    public class LongOperators : IQuantumOperators<long>
    {
        public long Add(long a, long b)
        {
            return a + b;
        }

        public long Subtract(long a, long b)
        {
            return a - b;
        }

        public long Multiply(long a, long b)
        {
            return a * b;
        }

        public long Divide(long a, long b)
        {
            return a / b;
        }

        public long Mod(long a, long b)
        {
            return a % b;
        }

        public bool GreaterThan(long a, long b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(long a, long b)
        {
            return a >= b;
        }

        public bool LessThan(long a, long b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(long a, long b)
        {
            return a <= b;
        }

        public bool Equal(long a, long b)
        {
            return a == b;
        }

        public bool NotEqual(long a, long b)
        {
            return a != b;
        }

        public bool IsAddCommutative => true;
    }
}
