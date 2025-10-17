namespace QuantumSuperposition.Utilities
{
    public class IntArrayComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y)
        {
            return x != null && y != null && x.SequenceEqual(y);
        }

        public int GetHashCode(int[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (int val in obj)
                {
                    hash = (hash * 31) + val;
                }
                return hash;
            }
        }
    }
}
