using System;

namespace PositronicVariables.Engine
{
    public interface IImprobabilityEngine<T>
        where T : IComparable<T>
    {
        void Run(
            Action code,
            bool runFinalIteration = true,
            bool unifyOnConvergence = true,
            bool bailOnFirstReverseWhenIdle = false,
            IImprobabilityEngine<T> next = null);
    }
}
