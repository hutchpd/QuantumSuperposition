using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public class AssignOperation<T>(PositronicVariable<T> variable, T assigned) : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; } = variable;
        public T Original { get; } = variable.GetCurrentQBit().ToCollapsedValues().First();

        public string OperationName => $"Assign {assigned}";

        /// <summary>
        /// Go back to whatever was once there after we started going forwards the first time
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T ApplyInverse(T result)
        {
            return Original;
        }

        /// <summary>
        /// Time travel as we know it on a day to day basis
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ApplyForward(T value)
        {
            return assigned;
        }
    }

}
