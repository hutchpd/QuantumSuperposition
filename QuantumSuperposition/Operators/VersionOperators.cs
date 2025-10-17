using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Because someone, somewhere, always has a better version than you.
    /// </summary>
    public class VersionOperators : IQuantumOperators<Version>
    {
        public Version Add(Version a, Version b)
        {
            throw new NotSupportedException("Adding versions creates cursed timelines.");
        }

        public Version Subtract(Version a, Version b)
        {
            throw new NotSupportedException("Subtracting versions leads to feature regression.");
        }

        public Version Multiply(Version a, Version b)
        {
            throw new NotSupportedException("Multiplying versions causes time paradoxes.");
        }

        public Version Divide(Version a, Version b)
        {
            throw new NotSupportedException("Dividing versions destroys continuity.");
        }

        public Version Mod(Version a, Version b)
        {
            throw new NotSupportedException("Modulo on versions? See a therapist.");
        }

        public bool GreaterThan(Version a, Version b)
        {
            return a > b;
        }

        public bool GreaterThanOrEqual(Version a, Version b)
        {
            return a >= b;
        }

        public bool LessThan(Version a, Version b)
        {
            return a < b;
        }

        public bool LessThanOrEqual(Version a, Version b)
        {
            return a <= b;
        }

        public bool Equal(Version a, Version b)
        {
            return a == b;
        }

        public bool NotEqual(Version a, Version b)
        {
            return a != b;
        }

        public bool IsAddCommutative => false;
    }
}
