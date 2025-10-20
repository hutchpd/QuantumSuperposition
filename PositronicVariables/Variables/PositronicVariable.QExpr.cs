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

            // Eager result
            internal readonly QuBit<T> Q;

            // Lazy materializer – when present, we build the QuBit at use-time
            private readonly Func<QuBit<T>> _lazy;
            private readonly bool _isLazy;


            internal QExpr(PositronicVariable<T> src, QuBit<T> q)
            {
                Source = src;
                Q = q;
                _lazy = null;
                _isLazy = false;
            }

            // ctor: build on demand from the Source's *current* qubit
            internal QExpr(PositronicVariable<T> src, Func<QuBit<T>> lazy)
            {
                Source = src;
                Q = default!;
                _lazy = lazy;
                _isLazy = true;
            }

            private QuBit<T> Resolve()
            {
                QuBit<T> qb = _isLazy ? _lazy() : Q;
                // ensure union-aware enumeration/printing
                return qb;
            }

            public IEnumerable<T> ToCollapsedValues()
            {
                return Resolve().ToCollapsedValues();
            }

            public override string ToString()
            {
                return Resolve().ToString();
            }

            public static implicit operator QuBit<T>(QExpr e)
            {
                return e.Resolve();
            }

            public static implicit operator PositronicVariable<T>(QExpr expr)
            {
                PositronicVariable<T> src = expr.Source ?? throw new InvalidOperationException("Detached QExpr has no source PositronicVariable.");
                src.Assign(expr);   // side-effectful assign
                return src;         // allow "antival = antival + 2;" to compile and mutate
            }

            // ---------- QExpr (x) scalar operators ----------
            public static QExpr operator %(QExpr left, T right)
            {
                QuBit<T> lazy()
                {
                    QuBit<T> resultQB = left.Resolve() % right;
                    resultQB.Any();
                    return resultQB;
                }

                if (left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new ReversibleModulusOp<T>(left.Source, right, left.Source._runtime));
                }

                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator +(QExpr left, T right)
            {
                QuBit<T> lazy()
                {
                    QuBit<T> resultQB = left.Resolve() + right;
                    resultQB.Any();
                    return resultQB;
                }

                if (left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new AdditionOperation<T>(left.Source, right, left.Source._runtime));
                }

                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator -(QExpr left, T right)
            {
                QuBit<T> lazy()
                {
                    QuBit<T> resultQB = left.Resolve() - right;
                    resultQB.Any();
                    return resultQB;
                }

                if (left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new SubtractionOperation<T>(left.Source, right, left.Source._runtime));
                }

                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator *(QExpr left, T right)
            {
                QuBit<T> resultQB = left.Resolve() * right;
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve() * right;
                    // qb.Any();  // QuBit operators typically call Any() where needed
                    return qb;
                }

                if (left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new MultiplicationOperation<T>(left.Source, right));
                }

                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator /(QExpr left, T right)
            {
                QuBit<T> lazy()
                {
                    QuBit<T> resultQB = left.Resolve() / right;
                    resultQB.Any();
                    return resultQB;
                }

                if (left.Source._runtime.Entropy >= 0)
                {
                    QuantumLedgerOfRegret.Record(new DivisionOperation<T>(left.Source, right, left.Source._runtime));
                }

                return new QExpr(left.Source, lazy);
            }

            // ---------- QExpr (x) bitwise with scalar ----------
            public static QExpr operator &(QExpr left, T right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve();
                    return qb.Select(v => PositronicVariables.Maths.Bitwise.And(v, right));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator |(QExpr left, T right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve();
                    return qb.Select(v => PositronicVariables.Maths.Bitwise.Or(v, right));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator ^(QExpr left, T right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve();
                    return qb.Select(v => PositronicVariables.Maths.Bitwise.Xor(v, right));
                }
                return new QExpr(left.Source, lazy);
            }

            // ---------- QExpr shifts and NOT ----------
            public static QExpr operator <<(QExpr left, int count)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve();
                    return qb.Select(v => PositronicVariables.Maths.Bitwise.ShiftLeft(v, count));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator >>(QExpr left, int count)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = left.Resolve();
                    return qb.Select(v => PositronicVariables.Maths.Bitwise.ShiftRight(v, count));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator ~(QExpr value)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> qb = value.Resolve();
                    return qb.Select(PositronicVariables.Maths.Bitwise.Not);
                }
                return new QExpr(value.Source, lazy);
            }

            // ---------- QExpr (x) PositronicVariable<T> (pairwise map) ----------
            public static QExpr operator &(QExpr left, PositronicVariable<T> right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.GetCurrentQBit();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.And(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator |(QExpr left, PositronicVariable<T> right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.GetCurrentQBit();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.Or(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator ^(QExpr left, PositronicVariable<T> right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.GetCurrentQBit();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.Xor(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }

            // ---------- QExpr (x) QExpr (pairwise map) ----------
            public static QExpr operator &(QExpr left, QExpr right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.Resolve();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.And(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator |(QExpr left, QExpr right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.Resolve();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.Or(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }

            public static QExpr operator ^(QExpr left, QExpr right)
            {
                EnsureIntegralBitwise();
                QuBit<T> lazy()
                {
                    QuBit<T> l = left.Resolve();
                    QuBit<T> r = right.Resolve();
                    return l.SelectMany(a => r.Select(b => PositronicVariables.Maths.Bitwise.Xor(a, b)));
                }
                return new QExpr(left.Source, lazy);
            }
        }
    }
}