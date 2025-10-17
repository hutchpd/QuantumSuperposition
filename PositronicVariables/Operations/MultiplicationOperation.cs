using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    /// <summary>
    /// Timey-wimey stuff. Multiplication in one direction becomes division in the other.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiplicationOperation<T>(PositronicVariable<T> variable, T multiplier) : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; } = variable;
        public T Original { get; } = variable.GetCurrentQBit().ToCollapsedValues().First();

        public string OperationName => $"Multiplication by {multiplier}";

        /// <summary>
        /// When time goes backwards the multiplication becomes a division.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Arithmetic.Divide(result, multiplier);
        }

        /// <summary>
        /// Otherwise we get out the multiplication table.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Multiply(value, multiplier);
        }
    }
}
