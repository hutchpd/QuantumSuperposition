using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantumSuperposition.QuantumSoup
{

    public struct CommutativeKey<T>
    {
        public T A { get; }
        public T B { get; }

        public CommutativeKey(T a, T b)
        {
            // If a and b are equal, the order doesn’t matter.
            // If they differ, we "order" them based on hash code (or you can require IComparable<T> for a more robust solution)
            if (Comparer<T>.Default.Compare(a, b) <= 0)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is not CommutativeKey<T> other) return false;
            return EqualityComparer<T>.Default.Equals(A, other.A) &&
                   EqualityComparer<T>.Default.Equals(B, other.B);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (A?.GetHashCode() ?? 0);
                hash = hash * 31 + (B?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
