using QuantumSuperposition.Core;
using System.Text;

namespace QuantumSuperposition.Operators
{
    public class StringOperators : IQuantumOperators<string>
    {
        // Define addition as string concatenation.
        public string Add(string a, string b) => a + b;

        // Define subtraction as removing the first occurrence of 'b' from 'a'.
        public string Subtract(string a, string b)
        {
            if (string.IsNullOrEmpty(b))
                return a;
            int index = a.IndexOf(b, StringComparison.Ordinal);
            if (index < 0)
                return a; // or optionally, throw an exception if subtraction should be "mandatory"
            return a.Remove(index, b.Length);
        }

        public string Multiply(string a, string b)
        {
            // Cartesian product of strings
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return string.Empty;
            var result = new StringBuilder();
            foreach (char c1 in a)
            {
                foreach (char c2 in b)
                {
                    result.Append(c1);
                    result.Append(c2);
                }
            }
            return result.ToString();
        }
        // Opposite of multiplication
        public string Divide(string a, string b)
                    => throw new NotSupportedException("Divide is not supported for strings.");

        // Modulo is not applicable.
        public string Mod(string a, string b)
            => throw new NotSupportedException("Modulo is not supported for strings.");

        // Comparisons: using ordinal comparison here; you may adjust for culture-specific needs.
        public bool GreaterThan(string a, string b) => string.Compare(a, b, StringComparison.Ordinal) > 0;
        public bool GreaterThanOrEqual(string a, string b) => string.Compare(a, b, StringComparison.Ordinal) >= 0;
        public bool LessThan(string a, string b) => string.Compare(a, b, StringComparison.Ordinal) < 0;
        public bool LessThanOrEqual(string a, string b) => string.Compare(a, b, StringComparison.Ordinal) <= 0;

        public bool Equal(string a, string b) => a == b;
        public bool NotEqual(string a, string b) => a != b;

        // String concatenation is not commutative.
        public bool IsAddCommutative => false;
    }
}
