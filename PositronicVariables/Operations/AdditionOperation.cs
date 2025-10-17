using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;
namespace PositronicVariables.Operations
{
    /// <summary>
    /// Multiversal maths isn't as hard as it looks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AdditionOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public T Addend { get; }
        public T Original { get; }

        public string OperationName => $"Addition of {Addend}";
        private readonly IPositronicRuntime _rt;

        public AdditionOperation(PositronicVariable<T> variable, T addend, IPositronicRuntime rt)
        {
            Variable = variable;
            Addend = addend;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();   // snapshot
            _rt = rt;
        }

        /// <summary>
        /// When time goes backwards the addition becomes a subtraction.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Arithmetic.Subtract(result, Addend);
        }

        /// <summary>
        /// And going forwards is just the regular old addition we all know and love.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyForward(T result)
        {
            return Arithmetic.Add(result, Addend);
        }
    }
}
