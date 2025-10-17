using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Float’s overachieving big sibling.
    /// Handles double the precision, double the existential crises.
    /// </summary>
    public class DoubleOperators : IQuantumOperators<double>
    {
        public double Add(double a, double b)
        {
            return a + b;
        }

        public double Subtract(double a, double b)
        {
            return a - b;
        }

        public double Multiply(double a, double b)
        {
            return a * b;
        }

        public double Divide(double a, double b)
        {
            return a / b;
        }

        public double Mod(double a, double b)
        {
            return a % b;
        }

        public bool GreaterThan(double a, double b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(double a, double b)
        {
            return a >= b;
        }

        public bool LessThan(double a, double b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(double a, double b)
        {
            return a <= b;
        }

        public bool Equal(double a, double b)
        {
            return a == b;
        }

        public bool NotEqual(double a, double b)
        {
            return a != b;
        }

        public bool IsAddCommutative => true;
    }
}
