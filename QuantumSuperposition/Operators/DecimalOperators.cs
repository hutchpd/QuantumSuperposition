using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// For when float and double just aren’t serious enough about their financial responsibilities.
    /// </summary>
    public class DecimalOperators : IQuantumOperators<decimal>
    {
        public decimal Add(decimal a, decimal b) => a + b;
        public decimal Subtract(decimal a, decimal b) => a - b;
        public decimal Multiply(decimal a, decimal b) => a * b;
        public decimal Divide(decimal a, decimal b) => a / b;
        public decimal Mod(decimal a, decimal b) => a % b;

        public bool GreaterThan(decimal a, decimal b) => a > b;
        public bool GreaterThanOrEqual(decimal a, decimal b) => a >= b;
        public bool LessThan(decimal a, decimal b) => a < b;
        public bool LessThanOrEqual(decimal a, decimal b) => a <= b;
        public bool Equal(decimal a, decimal b) => a == b;
        public bool NotEqual(decimal a, decimal b) => a != b;

        public bool IsAddCommutative => true;
    }
}
