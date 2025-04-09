using QuantumSuperposition.Core;
using System.Numerics;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Like a quantum therapist, but for complex numbers. Helps them add, subtract, and multiply their feelings.
    /// </summary>
    public class ComplexOperators : IQuantumOperators<Complex>
    {
        public Complex Add(Complex a, Complex b) => a + b;
        public Complex Subtract(Complex a, Complex b) => a - b;
        public Complex Multiply(Complex a, Complex b) => a * b;
        public Complex Divide(Complex a, Complex b) => a / b;

        // Modulo isn't mathematically defined for Complex, so we throw if attempted
        public Complex Mod(Complex a, Complex b) => throw new NotSupportedException("Modulus not supported for complex numbers.");

        // Comparisons on complex numbers are ambiguous, but we’ll define some heuristics based on magnitude:
        public bool GreaterThan(Complex a, Complex b) => a.Magnitude > b.Magnitude;
        public bool GreaterThanOrEqual(Complex a, Complex b) => a.Magnitude >= b.Magnitude;
        public bool LessThan(Complex a, Complex b) => a.Magnitude < b.Magnitude;
        public bool LessThanOrEqual(Complex a, Complex b) => a.Magnitude <= b.Magnitude;
        public bool Equal(Complex a, Complex b) => a == b;
        public bool NotEqual(Complex a, Complex b) => a != b;
        public bool IsAddCommutative => true;
    }
}
