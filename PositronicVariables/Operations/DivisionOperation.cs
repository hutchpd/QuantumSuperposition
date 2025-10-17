using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public class DivisionOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        private readonly T _divisor;
        private readonly IPositronicRuntime _rt;
        public T Original { get; }

        public string OperationName => $"Division by {_divisor}";

        public DivisionOperation(PositronicVariable<T> variable, T divisor, IPositronicRuntime rt)
        {
            Variable = variable;
            _divisor = divisor;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            _rt = rt;
        }
        /// <summary>
        /// When time goes backwards the division becomes a multiplication.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Arithmetic.Multiply(result, _divisor);
        }

        /// <summary>
        /// Otherwise we try and remember what long division was like, something to do with shifting one column to the right and then I get confused.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Divide(value, _divisor);
        }
    }
}
