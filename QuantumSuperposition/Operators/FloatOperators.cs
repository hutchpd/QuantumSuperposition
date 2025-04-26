using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Like a gym trainer, but lighter. Lifts floats instead of ints.
    /// May skip leg day, but compensates with decimals.
    /// </summary>
    public class FloatOperators : IQuantumOperators<float>
    {
        public float Add(float a, float b) => a + b;
        public float Subtract(float a, float b) => a - b;
        public float Multiply(float a, float b) => a * b;
        public float Divide(float a, float b) => a / b;
        public float Mod(float a, float b) => a % b;

        public bool GreaterThan(float a, float b) => a > b;
        public bool GreaterThanOrEqual(float a, float b) => a >= b;
        public bool LessThan(float a, float b) => a < b;
        public bool LessThanOrEqual(float a, float b) => a <= b;
        public bool Equal(float a, float b) => a == b;
        public bool NotEqual(float a, float b) => a != b;

        public bool IsAddCommutative => true;
    }
}
