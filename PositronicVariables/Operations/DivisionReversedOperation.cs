using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public class DivisionReversedOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        private readonly T _numerator;
        private readonly IPositronicRuntime _rt;
        public T Original { get; }

        public string OperationName => $"DivisionReversed with numerator {_numerator}";

        public DivisionReversedOperation(PositronicVariable<T> variable, T numerator, IPositronicRuntime rt)
        {
            Variable = variable;
            _numerator = numerator;
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
            return Arithmetic.Divide(_numerator, result);
        }

        /// <summary>
        /// Otherwise we try and remember what long division was like, something to do with shifting one column to the right and then I get confused.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Divide(_numerator, value);
        }
    }
}
