using PositronicVariables.Maths;
using PositronicVariables.Operations;
using QuantumSuperposition.QuantumSoup; // QuBit<T>
using System;
using System.Linq;

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
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new BitwiseAndOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.And(v, right)));
        }

        // v ^ c
        public static QExpr operator ^(PositronicVariable<T> left, T right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new BitwiseXorOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.Xor(v, right)));
        }

        // ~v
        public static QExpr operator ~(PositronicVariable<T> value)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && value._runtime.Entropy >= 0)
            {
                value._ops.Record(new BitwiseNotOperation<T>(value, value._runtime));
            }
            return new QExpr(value, () => value.GetCurrentQBit().Select(Bitwise.Not));
        }

        // v << n
        public static QExpr operator <<(PositronicVariable<T> left, int count)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new ShiftLeftOperation<T>(left, count, left._runtime));
            }
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.ShiftLeft(v, count)));
        }

        // v >> n
        public static QExpr operator >>(PositronicVariable<T> left, int count)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new ShiftRightOperation<T>(left, count, left._runtime));
            }
            return new QExpr(left, () => left.GetCurrentQBit().Select(v => Bitwise.ShiftRight(v, count)));
        }

        // v | c (now real bitwise OR)
        public static QExpr operator |(PositronicVariable<T> left, T right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new BitwiseOrOperation<T>(left, right, left._runtime));
            }
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
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new BitwiseAndOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.And(a, b))));
        }

        public static QExpr operator |(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new BitwiseOrOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.Or(a, b))));
        }

        public static QExpr operator ^(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new BitwiseXorOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, () =>
                left.GetCurrentQBit().SelectMany(a => right.GetCurrentQBit().Select(b => Bitwise.Xor(a, b))));
        }

        // --- NEW: scalar on the left (enables: 0 | v in Aggregate) ---

        public static QExpr operator &(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new BitwiseAndOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.And(left, v)));
        }

        public static QExpr operator |(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new BitwiseOrOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.Or(left, v)));
        }

        public static QExpr operator ^(T left, PositronicVariable<T> right)
        {
            EnsureIntegralBitwise();
            if (!s_SuppressOperatorLogging.Value && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new BitwiseXorOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, () => right.GetCurrentQBit().Select(v => Bitwise.Xor(left, v)));
        }
    }
}