using PositronicVariables.Maths;
using System;

namespace PositronicVariables.Variables
{
    public partial class PositronicVariable<T>
        where T : IComparable<T>
    {
        private static void EnsureIntegralBitwise()
        {
            if (!s_IsIntegralType)
                throw new NotSupportedException($"Bitwise operations are only supported for integral types. Type '{typeof(T)}' is not supported.");
        }

        // v & c
        public static QExpr operator &(PositronicVariable<T> left, T right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.And(v, right)));
        }

        // v ^ c
        public static QExpr operator ^(PositronicVariable<T> left, T right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.Xor(v, right)));
        }

        // ~v
        public static QExpr operator ~(PositronicVariable<T> value)
        {
            EnsureIntegralBitwise();
            return new QExpr(value, () => value.GetCurrentQBit().Select(Bitwise.Not));
        }

        // v << n
        public static QExpr operator <<(PositronicVariable<T> left, int count)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.ShiftLeft(v, count)));
        }

        // v >> n
        public static QExpr operator >>(PositronicVariable<T> left, int count)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.ShiftRight(v, count)));
        }

        // v | c (now real bitwise OR)
        public static QExpr operator |(PositronicVariable<T> left, T right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.Or(v, right)));
        }
    }
}