using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public sealed class BitwiseOrOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public T Operand { get; }
        public T Original { get; }

        public string OperationName => $"Bitwise OR with {Operand}";

        public BitwiseOrOperation(PositronicVariable<T> variable, T operand, IPositronicRuntime _)
        {
            Variable = variable;
            Operand = operand;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        }

        // Not invertible in general: restore snapshot
        public T ApplyInverse(T result) => Original;

        public T ApplyForward(T value) => Bitwise.Or(value, Operand);
    }

}