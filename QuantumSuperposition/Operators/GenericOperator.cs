using QuantumSuperposition.Core;
using System.Linq.Expressions;
using System.Numerics;

namespace QuantumSuperposition.Operators
{
    /// <summary>
    /// .NET 8 generic math implementation of <see cref="IQuantumOperators{T}"/> for any <see cref="INumber{T}"/> type.
    /// </summary>
    public class NumberOperators<T> : IQuantumOperators<T>
        where T : INumber<T>
    {
        public static readonly NumberOperators<T> Instance = new();

        public T Add(T a, T b) => a + b;
        public T Subtract(T a, T b) => a - b;
        public T Multiply(T a, T b) => a * b;
        public T Divide(T a, T b) => a / b;
        public T Mod(T a, T b) => a % b;
        public bool GreaterThan(T a, T b) => a > b;
        public bool GreaterThanOrEqual(T a, T b) => a >= b;
        public bool LessThan(T a, T b) => a < b;
        public bool LessThanOrEqual(T a, T b) => a <= b;
        public bool Equal(T a, T b) => a == b;
        public bool NotEqual(T a, T b) => a != b;
        public bool IsAddCommutative => true;
    }

    public static class GenericOperator<T>
    {
        public static readonly Func<T, T, T> Add = GenerateBinaryOperator(Expression.Add);
        public static readonly Func<T, T, T> Subtract = GenerateBinaryOperator(Expression.Subtract);
        public static readonly Func<T, T, T> Multiply = GenerateBinaryOperator(Expression.Multiply);
        public static readonly Func<T, T, T> Divide = GenerateBinaryOperator(Expression.Divide);
        public static readonly Func<T, T, T> Mod = GenerateBinaryOperator(Expression.Modulo);
        public static readonly Func<T, T, bool> GreaterThan = GenerateComparisonOperator(Expression.GreaterThan);
        public static readonly Func<T, T, bool> GreaterThanOrEqual = GenerateComparisonOperator(Expression.GreaterThanOrEqual);
        public static readonly Func<T, T, bool> LessThan = GenerateComparisonOperator(Expression.LessThan);
        public static readonly Func<T, T, bool> LessThanOrEqual = GenerateComparisonOperator(Expression.LessThanOrEqual);

        private static Func<T, T, T> GenerateBinaryOperator(Func<Expression, Expression, BinaryExpression> op)
        {
            ParameterExpression paramA = Expression.Parameter(typeof(T));
            ParameterExpression paramB = Expression.Parameter(typeof(T));
            try
            {
                BinaryExpression body = op(paramA, paramB);
                return Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
            }
            catch
            {
                return (a, b) => throw new NotSupportedException($"Operation not supported for type {typeof(T)}.");
            }
        }

        private static Func<T, T, bool> GenerateComparisonOperator(Func<Expression, Expression, BinaryExpression> op)
        {
            ParameterExpression paramA = Expression.Parameter(typeof(T));
            ParameterExpression paramB = Expression.Parameter(typeof(T));
            try
            {
                BinaryExpression body = op(paramA, paramB);
                return Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
            }
            catch
            {
                return (a, b) => throw new NotSupportedException($"Comparison not supported for type {typeof(T)}.");
            }
        }
    }

    public class ExpressionOperators<T> : IQuantumOperators<T>
    {
        public T Add(T a, T b) => GenericOperator<T>.Add(a, b);
        public T Subtract(T a, T b) => GenericOperator<T>.Subtract(a, b);
        public T Multiply(T a, T b) => GenericOperator<T>.Multiply(a, b);
        public T Divide(T a, T b) => GenericOperator<T>.Divide(a, b);
        public T Mod(T a, T b) => GenericOperator<T>.Mod(a, b);
        public bool GreaterThan(T a, T b) => GenericOperator<T>.GreaterThan(a, b);
        public bool GreaterThanOrEqual(T a, T b) => GenericOperator<T>.GreaterThanOrEqual(a, b);
        public bool LessThan(T a, T b) => GenericOperator<T>.LessThan(a, b);
        public bool LessThanOrEqual(T a, T b) => GenericOperator<T>.LessThanOrEqual(a, b);
        public bool Equal(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
        public bool NotEqual(T a, T b) => !EqualityComparer<T>.Default.Equals(a, b);
        public bool IsAddCommutative => true;
    }

}
