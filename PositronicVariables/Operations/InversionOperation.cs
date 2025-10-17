using PositronicVariables.Engine.Logging;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;

namespace PositronicVariables.Operations
{
    public class InversionOperation<T>(PositronicVariable<T> variable, T originalValue, T invertedValue, string opName) : IOperation
        where T : IComparable<T>
    {
        private readonly PositronicVariable<T> _variable = variable;
        private readonly T _originalValue = originalValue;
        private readonly T _invertedValue = invertedValue;
        public string OperationName { get; } = $"Inverse of {opName}";

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
