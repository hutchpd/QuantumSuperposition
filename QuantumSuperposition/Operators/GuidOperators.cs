using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// The sacred scrolls of unique identifiers. Not designed for arithmetic, just judgement.
    /// </summary>
    public class GuidOperators : IQuantumOperators<Guid>
    {
        public Guid Add(Guid a, Guid b) => throw new NotSupportedException("Adding GUIDs is heresy.");
        public Guid Subtract(Guid a, Guid b) => throw new NotSupportedException("Subtracting GUIDs fractures spacetime.");
        public Guid Multiply(Guid a, Guid b) => throw new NotSupportedException("GUID multiplication: please seek help.");
        public Guid Divide(Guid a, Guid b) => throw new NotSupportedException("GUID division not even defined in the multiverse.");
        public Guid Mod(Guid a, Guid b) => throw new NotSupportedException("Modulo on GUIDs? What dark ritual is this?");

        public bool GreaterThan(Guid a, Guid b) => a.CompareTo(b) > 0;
        public bool GreaterThanOrEqual(Guid a, Guid b) => a.CompareTo(b) >= 0;
        public bool LessThan(Guid a, Guid b) => a.CompareTo(b) < 0;
        public bool LessThanOrEqual(Guid a, Guid b) => a.CompareTo(b) <= 0;
        public bool Equal(Guid a, Guid b) => a == b;
        public bool NotEqual(Guid a, Guid b) => a != b;

        public bool IsAddCommutative => false;
    }
}
