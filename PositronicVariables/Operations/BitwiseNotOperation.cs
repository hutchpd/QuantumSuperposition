using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public sealed class BitwiseNotOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public T Original { get; }

        public string OperationName => "Bitwise NOT";

        public BitwiseNotOperation(PositronicVariable<T> variable, IPositronicRuntime _)
        {
            Variable = variable;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        }

        // NOT is self-inverse
        public T ApplyInverse(T result) => Bitwise.Not(result);

        public T ApplyForward(T value) => Bitwise.Not(value);
    }
}