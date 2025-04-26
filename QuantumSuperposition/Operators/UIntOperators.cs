using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// The reliable middle manager of integers. Unsigned, unbiased.
    /// </summary>
    public class UIntOperators : IQuantumOperators<uint>
    {
        public uint Add(uint a, uint b) => a + b;
        public uint Subtract(uint a, uint b) => a - b;
        public uint Multiply(uint a, uint b) => a * b;
        public uint Divide(uint a, uint b) => a / b;
        public uint Mod(uint a, uint b) => a % b;

        public bool GreaterThan(uint a, uint b) => a > b;
        public bool GreaterThanOrEqual(uint a, uint b) => a >= b;
        public bool LessThan(uint a, uint b) => a < b;
        public bool LessThanOrEqual(uint a, uint b) => a <= b;
        public bool Equal(uint a, uint b) => a == b;
        public bool NotEqual(uint a, uint b) => a != b;

        public bool IsAddCommutative => true;
    }
}
