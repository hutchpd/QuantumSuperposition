using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    /// <summary>
    /// If this looks a lot like calling addition something else, that's because it is.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SubtractionReversedOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        private readonly T _minuend;
        private readonly IPositronicRuntime _rt;
        public T Original { get; }

        public string OperationName => $"SubtractionReversed with minuend {_minuend}";


        public SubtractionReversedOperation(PositronicVariable<T> variable, T minuend, IPositronicRuntime rt)
        {
            Variable = variable;
            _minuend = minuend;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            _rt = rt;
        }

        /// <summary>
        /// It's like regular subtraction, but backwards. So we reverse it by subtracting from the minuend.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Arithmetic.Subtract(_minuend, result);
        }

        /// <summary>
        /// And going forwards is just the regular old subtraction we all know and love.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return Arithmetic.Subtract(_minuend, value);
        }
    }
}
