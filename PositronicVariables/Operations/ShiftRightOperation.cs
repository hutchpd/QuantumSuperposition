using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public sealed class ShiftRightOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public int Count { get; }
        public T Original { get; }

        public string OperationName => $"Shift Right by {Count}";

        public ShiftRightOperation(PositronicVariable<T> variable, int count, IPositronicRuntime _)
        {
            Variable = variable;
            Count = count;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        }

        // Not invertible in general (bit loss): restore snapshot
        public T ApplyInverse(T result) => Original;

        public T ApplyForward(T value) => Bitwise.ShiftRight(value, Count);
    }
}