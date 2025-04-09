using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Operators for boolean values. Because why not?
    /// </summary>
    public class BooleanOperators : IQuantumOperators<bool>
    {
        public bool Add(bool a, bool b) => a || b; // logical OR
        public bool Subtract(bool a, bool b) => a && !b; // arbitrary definition
        public bool Multiply(bool a, bool b) => a && b; // logical AND
        public bool Divide(bool a, bool b) => throw new NotSupportedException("Division not supported for booleans, why are you trying to divide truth?.");
        public bool Mod(bool a, bool b) => throw new NotSupportedException("I don't even know what that could possibly mean.");
        public bool GreaterThan(bool a, bool b) => false; // not well-defined
        public bool GreaterThanOrEqual(bool a, bool b) => a == b; // or false
        public bool LessThan(bool a, bool b) => false;
        public bool LessThanOrEqual(bool a, bool b) => a == b;
        public bool Equal(bool a, bool b) => a == b;
        public bool NotEqual(bool a, bool b) => a != b;
        public bool IsAddCommutative => true;

    }
}
