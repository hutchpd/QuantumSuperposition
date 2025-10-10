using PositronicVariables.Engine.Logging;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;
using System;

namespace PositronicVariables.Operations.Interfaces
{
    public interface IReversibleSnapshotOperation<T> : IReversibleOperation<T>
        where T : IComparable<T>
    {
        PositronicVariable<T> Variable { get; }
        T Original { get; }
        void IOperation.Undo()
        {
            var qb = new QuBit<T>(new[] { Original });
            qb.Any();
            // Magically swap out the latest cosmic crumb for something more fitting of this reality
            Variable.timeline[Variable.timeline.Count - 1] = qb;
        }
    }
}
