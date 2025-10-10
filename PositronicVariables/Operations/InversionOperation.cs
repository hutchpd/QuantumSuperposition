using PositronicVariables.Engine.Logging;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;

namespace PositronicVariables.Operations
{
    public class InversionOperation<T> : IOperation
        where T : IComparable<T>
    {
        private readonly PositronicVariable<T> _variable;
        private readonly T _originalValue;
        private readonly T _invertedValue;
        private readonly IPositronicRuntime _rt;
        public string OperationName { get; }

        public InversionOperation(PositronicVariable<T> variable, T originalValue, T invertedValue, string opName, IPositronicRuntime rt)
        {
            _variable = variable;
            _originalValue = originalValue;
            _invertedValue = invertedValue;
            OperationName = $"Inverse of {opName}";
            _rt = rt;
        }

        /// <summary>
        /// When time moves forward, we apply the inversion.
        /// </summary>
        public void Undo()
        {
            // To undo an inversion, reassign the original forward value.
            _variable.Assign(_originalValue);
        }
    }
}
