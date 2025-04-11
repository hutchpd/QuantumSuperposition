using QuantumSuperposition.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QuantumSuperposition.Operators
{
    public static class GenericOperator<T>
    {
        public static readonly Func<T, T, T> Add = GenerateBinaryOperator(Expression.Add);
        public static readonly Func<T, T, T> Subtract = GenerateBinaryOperator(Expression.Subtract);
        public static readonly Func<T, T, T> Multiply = GenerateBinaryOperator(Expression.Multiply);
        public static readonly Func<T, T, T> Divide = GenerateBinaryOperator(Expression.Divide);

        private static Func<T, T, T> GenerateBinaryOperator(Func<Expression, Expression, BinaryExpression> op)
        {
            var paramA = Expression.Parameter(typeof(T));
            var paramB = Expression.Parameter(typeof(T));
            try
            {
                var body = op(paramA, paramB);
                return Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
            }
            catch
            {
                return (a, b) => throw new NotSupportedException($"Operation not supported for type {typeof(T)}.");
            }
        }
    }

    public class ExpressionOperators<T> : IQuantumOperators<T>
    {
        public T Add(T a, T b) => GenericOperator<T>.Add(a, b);
        public T Subtract(T a, T b) => GenericOperator<T>.Subtract(a, b);
        public T Multiply(T a, T b) => GenericOperator<T>.Multiply(a, b);
        public T Divide(T a, T b) => GenericOperator<T>.Divide(a, b);
        public T Mod(T a, T b) => throw new NotSupportedException(); // Not supported in expressions

        public bool GreaterThan(T a, T b) => throw new NotSupportedException(); // Extendable!
        public bool GreaterThanOrEqual(T a, T b) => throw new NotSupportedException();
        public bool LessThan(T a, T b) => throw new NotSupportedException();
        public bool LessThanOrEqual(T a, T b) => throw new NotSupportedException();
        public bool Equal(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
        public bool NotEqual(T a, T b) => !EqualityComparer<T>.Default.Equals(a, b);

        public bool IsAddCommutative => true;
    }

}
