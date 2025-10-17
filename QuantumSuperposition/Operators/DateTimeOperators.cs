using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Time-travel not included. Handles temporal gymnastics for DateTime.
    /// </summary>
    public class DateTimeOperators : IQuantumOperators<DateTime>
    {
        public DateTime Add(DateTime a, DateTime b)
        {
            return a + (b - DateTime.MinValue);
        }

        public DateTime Subtract(DateTime a, DateTime b)
        {
            return a - (b - DateTime.MinValue);
        }

        public DateTime Multiply(DateTime a, DateTime b)
        {
            throw new NotSupportedException("Multiplying time is a sci-fi feature. Coming soon: never.");
        }

        public DateTime Divide(DateTime a, DateTime b)
        {
            throw new NotSupportedException("Dividing time breaks causality. Please refrain.");
        }

        public DateTime Mod(DateTime a, DateTime b)
        {
            throw new NotSupportedException("Modulo time? Are you *trying* to invent time loops?");
        }

        public bool GreaterThan(DateTime a, DateTime b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(DateTime a, DateTime b)
        {
            return a >= b;
        }

        public bool LessThan(DateTime a, DateTime b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(DateTime a, DateTime b)
        {
            return a <= b;
        }

        public bool Equal(DateTime a, DateTime b)
        {
            return a == b;
        }

        public bool NotEqual(DateTime a, DateTime b)
        {
            return a != b;
        }

        public bool IsAddCommutative => false; // order of time addition matters, usually
    }
}
