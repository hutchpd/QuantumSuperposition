using QuantumSuperposition.Core;


namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Like a gym trainer, but for ints. Does the usual heavy lifting.
    /// </summary>
    public class IntOperators : IQuantumOperators<int>
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Multiply(int a, int b) => a * b;
        public int Divide(int a, int b) => a / b;
        public int Mod(int a, int b) => a % b;
        public bool GreaterThan(int a, int b) => a > b;
        public bool GreaterThanOrEqual(int a, int b) => a >= b;
        public bool LessThan(int a, int b) => a < b;
        public bool LessThanOrEqual(int a, int b) => a <= b;
        public bool Equal(int a, int b) => a == b;
        public bool NotEqual(int a, int b) => a != b;
        public bool IsAddCommutative => true;
    }
}
