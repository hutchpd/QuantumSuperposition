using PositronicVariables.Engine.Logging;
using PositronicVariables.Maths;
using PositronicVariables.Operations;
using QuantumSuperposition.QuantumSoup;
using System;
using System.Collections.Generic;

namespace PositronicVariables.Variables
{
    public partial class PositronicVariable<T>
        where T : IComparable<T>
    {
        /// <summary>
        /// A sentient wrapper that delays reality itself.
        /// Useful for pretending your math is correct until it's too late to stop it.
        /// </summary>
        public readonly struct QExpr
        {
            internal readonly PositronicVariable<T> Source;

            internal readonly QuBit<T> Q;

            private readonly Func<QuBit<T>> _lazy;
            private readonly QuBit<T>? _eager;

            internal QExpr(PositronicVariable<T> src, Func<QuBit<T>> lazy)
            {
                Source = src ?? throw new ArgumentNullException(nameof(src));
                _lazy = lazy ?? throw new ArgumentNullException(nameof(lazy));
                _eager = null;
            }

            internal QExpr(PositronicVariable<T> src, QuBit<T> q)
            {
                Source = src ?? throw new ArgumentNullException(nameof(src));
                _lazy = null!;
                _eager = q ?? throw new ArgumentNullException(nameof(q));
            }

            private QuBit<T> Resolve()
            {
                if (_eager is not null) return _eager;
                QuBit<T> qb = _lazy();
                return qb;
            }

            public static implicit operator QuBit<T>(QExpr e) => e.Resolve();

            public IEnumerable<T> ToCollapsedValues() => Resolve().ToCollapsedValues();
            public override string ToString() => Resolve().ToString();

            public static implicit operator PositronicVariable<T>(QExpr expr)
            {
                PositronicVariable<T> src = expr.Source ?? throw new InvalidOperationException("Detached QExpr has no source PositronicVariable.");
                src.Assign(expr);
                return src;
            }

            // ---------- QExpr (x) scalar operators ----------
            public static QExpr operator +(QExpr left, T right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left.Resolve() + right;
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left.Source, right, left.Source._runtime));
                }
                return new QExpr(left.Source, Lazy);
            }

            public static QExpr operator +(T left, QExpr right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left + right.Resolve();
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && right.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new AdditionOperation<T>(right.Source, left, right.Source._runtime));
                }
                return new QExpr(right.Source, Lazy);
            }

            public static QExpr operator -(QExpr left, T right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left.Resolve() - right;
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left.Source, right, left.Source._runtime));
                }
                return new QExpr(left.Source, Lazy);
            }

            public static QExpr operator -(T left, QExpr right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left - right.Resolve();
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && right.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new SubtractionReversedOperation<T>(right.Source, left, right.Source._runtime));
                }
                return new QExpr(right.Source, Lazy);
            }

            public static QExpr operator *(QExpr left, T right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left.Resolve() * right;
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(
                        IsMinus1(right)
                            ? new NegationOperation<T>(left.Source)
                            : new MultiplicationOperation<T>(left.Source, right));
                }
                return new QExpr(left.Source, Lazy);
            }

            public static QExpr operator *(T left, QExpr right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left * right.Resolve();
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && right.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(
                        IsMinus1(left)
                            ? new NegationOperation<T>(right.Source)
                            : new MultiplicationOperation<T>(right.Source, left));
                }
                return new QExpr(right.Source, Lazy);
            }

            public static QExpr operator /(QExpr left, T right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left.Resolve() / right;
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left.Source, right, left.Source._runtime));
                }
                return new QExpr(left.Source, Lazy);
            }

            public static QExpr operator /(T left, QExpr right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left / right.Resolve();
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && right.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new DivisionReversedOperation<T>(right.Source, left, right.Source._runtime));
                }
                return new QExpr(right.Source, Lazy);
            }

            public static QExpr operator %(QExpr left, T right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left.Resolve() % right;
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, right, left.Source._runtime));
                }
                return new QExpr(left.Source, Lazy);
            }

            public static QExpr operator %(T left, QExpr right)
            {
                QuBit<T> Lazy()
                {
                    QuBit<T> resultQB = left % right.Resolve();
                    resultQB.Any();
                    return resultQB;
                }

                if (!s_SuppressOperatorLogging && right.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(right.Source, left, right.Source._runtime));
                }
                return new QExpr(right.Source, Lazy);
            }
        }
    }
}