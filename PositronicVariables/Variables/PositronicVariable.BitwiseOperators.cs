using PositronicVariables.Maths;
using QuantumSuperposition.QuantumSoup; // QuBit<T>
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

        public static QExpr operator &(PositronicVariable<T> left, QExpr right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
            {
                QuBit<T> l = left.GetCurrentQBit();
                QuBit<T> r = right;
                return l.SelectMany(a => r.Select(b => Bitwise.And(a, b)));
            });
        }

        public static QExpr operator |(PositronicVariable<T> left, QExpr right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
            {
                QuBit<T> l = left.GetCurrentQBit();
                QuBit<T> r = right;
                return l.SelectMany(a => r.Select(b => Bitwise.Or(a, b)));
            });
        }

        public static QExpr operator ^(PositronicVariable<T> left, QExpr right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
            {
                QuBit<T> l = left.GetCurrentQBit();
                QuBit<T> r = right;
                return l.SelectMany(a => r.Select(b => Bitwise.Xor(a, b)));
            });
        }

        // --- Optional: PositronicVariable<T> op PositronicVariable<T> ---

        public static QExpr operator &(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.And(a, b))));
        }

        public static QExpr operator |(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.Or(a, b))));
        }

        public static QExpr operator ^(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.Xor(a, b))));
        }

        // --- NEW: scalar on the left (enables: 0 | v in Aggregate) ---

        public static QExpr operator &(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.And(left, v)));
        }

        public static QExpr operator |(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.Or(left, v)));
        }

        public static QExpr operator ^(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.Xor(left, v)));
        }
    }
}