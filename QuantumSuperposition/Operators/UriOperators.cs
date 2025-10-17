using QuantumSuperposition.Core;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// Attempts to math URLs. Results may vary wildly. Don't try this at home.
    /// </summary>
    public class UriOperators : IQuantumOperators<Uri>
    {
        public Uri Add(Uri a, Uri b)
        {
            return new Uri(a, b); // Combine a base URI + relative
        }

        public Uri Subtract(Uri a, Uri b)
        {
            throw new NotSupportedException("Subtracting URIs isn't a real thing unless you're Google.");
        }

        public Uri Multiply(Uri a, Uri b)
        {
            throw new NotSupportedException("Multiplying URIs risks portal instability.");
        }

        public Uri Divide(Uri a, Uri b)
        {
            throw new NotSupportedException("Dividing URIs just sounds wrong.");
        }

        public Uri Mod(Uri a, Uri b)
        {
            throw new NotSupportedException("Modulo on URIs?? HTTP 418 - I'm a teapot.");
        }

        public bool GreaterThan(Uri a, Uri b)
        {
            return string.Compare(a.AbsoluteUri, b.AbsoluteUri, StringComparison.Ordinal) > 0;
        }

        public bool GreaterThanOrEqual(Uri a, Uri b)
        {
            return string.Compare(a.AbsoluteUri, b.AbsoluteUri, StringComparison.Ordinal) >= 0;
        }

        public bool LessThan(Uri a, Uri b)
        {
            return string.Compare(a.AbsoluteUri, b.AbsoluteUri, StringComparison.Ordinal) < 0;
        }

        public bool LessThanOrEqual(Uri a, Uri b)
        {
            return string.Compare(a.AbsoluteUri, b.AbsoluteUri, StringComparison.Ordinal) <= 0;
        }

        public bool Equal(Uri a, Uri b)
        {
            return a.Equals(b);
        }

        public bool NotEqual(Uri a, Uri b)
        {
            return !a.Equals(b);
        }

        public bool IsAddCommutative => false;
    }
}
