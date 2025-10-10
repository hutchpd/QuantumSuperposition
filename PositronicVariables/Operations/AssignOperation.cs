using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public class AssignOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public T Original { get; }
        private readonly T _assigned;
        public string OperationName => $"Assign {_assigned}";

        public AssignOperation(PositronicVariable<T> variable, T assigned, IPositronicRuntime rt)
        {
            Variable = variable;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
            _assigned = assigned;
        }

        /// <summary>
        /// Go back to whatever was once there after we started going forwards the first time
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result) => Original;
        /// <summary>
        /// Time travel as we know it on a day to day basis
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value) => _assigned;
    }

}
