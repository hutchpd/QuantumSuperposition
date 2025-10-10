using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Operations
{
    /// <summary>
    /// If you walk backwards by a certain amount, you can reverse the effect of having walked forwards by that same amount.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SubtractionOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        private readonly T _subtrahend;
        public T Original { get; }

        public string OperationName => $"Subtraction of {_subtrahend}";
        private readonly IPositronicRuntime _rt;

        public SubtractionOperation(PositronicVariable<T> variable, T subtrahend, IPositronicRuntime rt)
        {
            Variable = variable;
            _subtrahend = subtrahend;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            _rt = rt;
        }

        /// <summary>
        /// You get the idea, right? Subtracting is just adding the negative, so we reverse it by adding.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result) => Arithmetic.Add(result, _subtrahend);
        /// <summary>
        /// And going forwards is just the regular old subtraction we all know and love.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value) => Arithmetic.Subtract(value, _subtrahend);

    }
}
