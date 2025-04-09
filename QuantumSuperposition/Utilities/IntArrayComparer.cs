using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantumSuperposition.Utilities
{
    public class IntArrayComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y)
        {
            if (x == null || y == null) return false;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(int[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (int val in obj)
                {
                    hash = hash * 31 + val;
                }
                return hash;
            }
        }
    }
}
