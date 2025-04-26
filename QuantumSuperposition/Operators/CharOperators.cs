using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Because sometimes you just need to compare angry letters.
    /// </summary>
    public class CharOperators : IQuantumOperators<char>
    {
        public char Add(char a, char b) => (char)(a + b);
        public char Subtract(char a, char b) => (char)(a - b);
        public char Multiply(char a, char b) => (char)(a * b);
        public char Divide(char a, char b) => (char)(a / b);
        public char Mod(char a, char b) => (char)(a % b);

        public bool GreaterThan(char a, char b) => a > b;
        public bool GreaterThanOrEqual(char a, char b) => a >= b;
        public bool LessThan(char a, char b) => a < b;
        public bool LessThanOrEqual(char a, char b) => a <= b;
        public bool Equal(char a, char b) => a == b;
        public bool NotEqual(char a, char b) => a != b;

        public bool IsAddCommutative => true;
    }
}
