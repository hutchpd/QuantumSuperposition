using System;
using System.Linq.Expressions;

namespace PositronicVariables.Runtime
{
    internal static class NumericOps<T>
    {
        private static readonly Func<T, T, T> s_add = CreateBinaryOp(Expression.Add, fallbackRight: true);
        private static readonly Func<T, T, T> s_sub = CreateBinaryOp(Expression.Subtract, fallbackRight: false);

        public static T Add(T a, T b) => s_add(a, b);
        public static T Subtract(T a, T b) => s_sub(a, b);

        private static Func<T, T, T> CreateBinaryOp(Func<Expression, Expression, BinaryExpression> op, bool fallbackRight)
        {
            try
            {
                ParameterExpression pA = Expression.Parameter(typeof(T), "a");
                ParameterExpression pB = Expression.Parameter(typeof(T), "b");
                Expression body;
                if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                {
                    body = Expression.Convert(
                        op(Expression.Convert(pA, typeof(int)), Expression.Convert(pB, typeof(int))),
                        typeof(T));
                }
                else
                {
                    body = op(pA, pB);
                }
                return Expression.Lambda<Func<T, T, T>>(body, pA, pB).Compile();
            }
            catch
            {
                return (a, b) =>
                {
                    try { dynamic da = a; dynamic db = b; return (T)(fallbackRight ? da + db : da - db); }
                    catch { return fallbackRight ? b : a; }
                };
            }
        }
    }
}
