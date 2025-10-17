using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public class NegationOperation<T>(PositronicVariable<T> variable) : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; } = variable;
        public T Original { get; } = variable.GetCurrentQBit().ToCollapsedValues().First();

        public string OperationName => "Negation";

        /// <summary>
        /// Negation is its own inverse, like a well-trained quantum ninja.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Arithmetic.Negate(result);
        }

        /// <summary>
        /// And going forwards is just the regular old negation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Negate(value);
        }
    }
}
