using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using System;
using System.Linq;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;

namespace PositronicVariables.Operations
{
    /// <summary>
    /// Timey-wimey stuff. Multiplication in one direction becomes division in the other.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiplicationOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        private readonly T _multiplier;
        private readonly IPositronicRuntime _rt;
        public T Original { get; }

        public string OperationName => $"Multiplication by {_multiplier}";

        public MultiplicationOperation(PositronicVariable<T> variable, T multiplier, IPositronicRuntime rt)
        {
            Variable = variable;
            _multiplier = multiplier;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            _rt = rt;
        }

        /// <summary>
        /// When time goes backwards the multiplication becomes a division.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result) => Arithmetic.Divide(result, _multiplier);
        /// <summary>
        /// Otherwise we get out the multiplication table.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value) => Arithmetic.Multiply(value, _multiplier);

    }
}
