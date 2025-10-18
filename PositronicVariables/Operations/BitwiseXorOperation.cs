using PositronicVariables.Maths;
using PositronicVariables.Operations.Interfaces;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;
using System.Linq;

namespace PositronicVariables.Operations
{
    public sealed class BitwiseXorOperation<T> : IReversibleSnapshotOperation<T>
        where T : IComparable<T>
    {
        public PositronicVariable<T> Variable { get; }
        public T Operand { get; }
        public T Original { get; }

        public string OperationName => $"Bitwise XOR with {Operand}";

        public BitwiseXorOperation(PositronicVariable<T> variable, T operand, IPositronicRuntime _)
        {
            Variable = variable;
            Operand = operand;
            Original = variable.GetCurrentQBit().ToCollapsedValues().First();
        }

        // XOR is self-inverse
        public T ApplyInverse(T result) => Bitwise.Xor(result, Operand);

        public T ApplyForward(T value) => Bitwise.Xor(value, Operand);
    }
}