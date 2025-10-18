using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public sealed class ShiftLeftOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public int Count { get; }
        public T Original { get; }

        public string OperationName => $"Shift Left by {Count}";

        public ShiftLeftOperation(PositronicVariable<T> variable, int count, IPositronicRuntime _)
        {
            Variable = variable;
            Count = count;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        }

        // Not invertible in general (bit loss): restore snapshot
        public T ApplyInverse(T result) => Original;

        public T ApplyForward(T value) => Bitwise.ShiftLeft(value, Count);
    }
}