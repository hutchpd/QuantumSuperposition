using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Small, but fierce. Bites at 0xFF strength.
    /// </summary>
    public class ByteOperators : IQuantumOperators<byte>
    {
        public byte Add(byte a, byte b) => (byte)(a + b);
        public byte Subtract(byte a, byte b) => (byte)(a - b);
        public byte Multiply(byte a, byte b) => (byte)(a * b);
        public byte Divide(byte a, byte b) => (byte)(a / b);
        public byte Mod(byte a, byte b) => (byte)(a % b);

        public bool GreaterThan(byte a, byte b) => a > b;
        public bool GreaterThanOrEqual(byte a, byte b) => a >= b;
        public bool LessThan(byte a, byte b) => a < b;
        public bool LessThanOrEqual(byte a, byte b) => a <= b;
        public bool Equal(byte a, byte b) => a == b;
        public bool NotEqual(byte a, byte b) => a != b;

        public bool IsAddCommutative => true;
    }
}
