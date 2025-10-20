using QuantumSuperposition.QuantumSoup;
using System;

namespace PositronicVariables.Variables
{
    public static class QSugar
    {
        // Usage: target <<= Q(10); or target <<= Q(7, 8, 9);
        public static QuBit<T> Q<T>(params T[] values) where T : IComparable<T>
        {
            var qb = new QuBit<T>(values);
            qb.Any(); // mark as superposition-friendly
            return qb;
        }
    }
}