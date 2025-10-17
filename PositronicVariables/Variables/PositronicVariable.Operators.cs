using System;

namespace PositronicVariables.Variables
{
    public partial class PositronicVariable<T>
        where T : IComparable<T>
    {
        // Scalar sugar: antival |= 10;  // identical to antival.Assign(10);
        // Also enables: antival = antival | 10;
        public static PositronicVariable<T> operator |(PositronicVariable<T> left, T right)
        {
            left.Assign(right);
            return left;
        }
    }
}