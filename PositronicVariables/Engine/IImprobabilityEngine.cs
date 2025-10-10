using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
