using PositronicVariables.Operations;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PositronicVariables.Variables
{
    public partial class PositronicVariable<T>
        where T : IComparable<T>
    {
        #region region Operator Overloads
        // --- Operator Overloads ---
        // --- Addition Overloads ---
        public static QExpr operator +(PositronicVariable<T> left, T right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = left.GetCurrentQBit();
                QuBit<T> resultQB = srcQB + right;
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new AdditionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, lazy);
        }

        public static QExpr operator +(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() + left;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new AdditionOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator +(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() + right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new AdditionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- QExpr-aware Addition Overloads ---
        public static QExpr operator +(PositronicVariable<T> left, QExpr right)
        {
            // Materialize the right expression's qubit; do not log here (ops already logged when building 'right')
            QuBit<T> resultQB = left.GetCurrentQBit() + (QuBit<T>)right;
            MarkAnyIfSuperposition(resultQB);
            return new QExpr(left, resultQB);
        }

        public static QExpr operator +(QExpr left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = (QuBit<T>)left + right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            // Use left.Source as the QExpr's source
            return new QExpr(left.Source, resultQB);
        }

        // --- Subtraction Overloads ---
        public static QExpr operator -(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() - right;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new SubtractionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator -(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() - left;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new SubtractionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator -(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() - right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new SubtractionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Unary Negation ---
        public static QExpr operator -(PositronicVariable<T> value)
        {
            QuBit<T> qb = value.GetCurrentQBit();
            T[] negatedValues = [.. qb.ToCollapsedValues().Select(v => { dynamic dv = v; return (T)(-dv); })];
            QuBit<T> negatedQb = new(negatedValues);
            MarkAnyIfSuperposition(negatedQb);

            return new QExpr(value, negatedQb);
        }

        // --- Multiplication Overloads ---

        public static QExpr operator *(PositronicVariable<T> left, T right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = left.GetCurrentQBit();
                QuBit<T> resultQB = srcQB * right;
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }

            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(
                    IsMinus1(right)
                        ? new NegationOperation<T>(left)
                        : new MultiplicationOperation<T>(left, right));
            }
            return new QExpr(left, lazy);
        }

        public static QExpr operator *(T left, PositronicVariable<T> right)
        {
            QuBit<T> lazy()
            {
                QuBit<T> srcQB = right.GetCurrentQBit();
                T[] multiplied = [.. srcQB
                    .ToCollapsedValues()
                    .Select(v => { dynamic dl = left; dynamic dv = v; return (T)(dl * dv); })
                    .Distinct()];
                QuBit<T> resultQB = new(multiplied);
                MarkAnyIfSuperposition(resultQB);
                return resultQB;
            }

            if (!(s_SuppressOperatorLogging.Value) && right._runtime.Entropy >= 0)
            {
                right._ops.Record(
                    IsMinus1(left)
                        ? new NegationOperation<T>(right)
                        : new MultiplicationOperation<T>(right, left));
            }
            return new QExpr(right, lazy);
        }

        // --- Division Overloads ---
        public static QExpr operator /(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() / right;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new DivisionOperation<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator /(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() / left;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new DivisionReversedOperation<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator /(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() / right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                T operand = right.GetCurrentQBit().ToCollapsedValues().First();
                left._ops.Record(new DivisionOperation<T>(left, operand, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Modulus Overloads ---
        public static QExpr operator %(PositronicVariable<T> left, T right)
        {
            QuBit<T> resultQB = left.GetCurrentQBit() % right;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new ReversibleModulusOp<T>(left, right, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        public static QExpr operator %(T left, PositronicVariable<T> right)
        {
            QuBit<T> resultQB = right.GetCurrentQBit() % left;
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && right._runtime.Entropy >= 0)
            {
                right._ops.Record(new ReversibleModulusOp<T>(right, left, right._runtime));
            }
            return new QExpr(right, resultQB);
        }

        public static QExpr operator %(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            T divisor = right.GetCurrentQBit().ToCollapsedValues().First();
            QuBit<T> resultQB = left.GetCurrentQBit() % right.GetCurrentQBit();
            MarkAnyIfSuperposition(resultQB);
            if (!(s_SuppressOperatorLogging.Value) && left._runtime.Entropy >= 0)
            {
                left._ops.Record(new ReversibleModulusOp<T>(left, divisor, left._runtime));
            }
            return new QExpr(left, resultQB);
        }

        // --- Comparison operators (Comparer<T>.Default) riding shotgun ---

        public static bool operator <(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) < 0;
        }
        public static bool operator >(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) > 0;
        }
        public static bool operator <=(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) <= 0;
        }
        public static bool operator >=(PositronicVariable<T> left, T right)
        {
            return Comparer<T>.Default.Compare(left.GetCurrentQBit().ToCollapsedValues().First(), right) >= 0;
        }
        public static bool operator <(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) < 0;
        }
        public static bool operator >(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) > 0;
        }
        public static bool operator <=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) <= 0;
        }
        public static bool operator >=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return Comparer<T>.Default.Compare(
                left.GetCurrentQBit().ToCollapsedValues().First(),
                right.GetCurrentQBit().ToCollapsedValues().First()) >= 0;
        }

        public static bool operator ==(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return ReferenceEquals(left, right) || (left is not null && right is not null && left.SameStates(left.GetCurrentQBit(), right.GetCurrentQBit()));
        }
        public static bool operator !=(PositronicVariable<T> left, PositronicVariable<T> right)
        {
            return !(left == right);
        }
        public static bool operator ==(PositronicVariable<T> left, T right)
        {
            return left.GetCurrentQBit().ToCollapsedValues().First().Equals(right);
        }
        public static bool operator !=(PositronicVariable<T> left, T right)
        {
            return !(left == right);
        }

        // ---------- PositronicVariable<T> ⊗ QuBit<T> / QExpr ----------
        /// <summary>
        /// This is not a bitwise shift; it's a ceremonial grafting of a QuBit onto a PositronicVariable.
        /// </summary>
        public static QExpr operator <<(PositronicVariable<T> target, QuBit<T> qb)
        {
            return new QExpr(target, qb);
        }

        /// <summary>
        /// This is not a bitwise shift; it's a ceremonial grafting of a QExpr onto a PositronicVariable.
        /// </summary>
        public static QExpr operator <<(PositronicVariable<T> target, QExpr expr)
        {
            return new QExpr(target, () => (QuBit<T>)expr);
        }
        #endregion
    }
}
