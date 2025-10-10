using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine
{
    public class ChroniclerDrive<T> : IImprobabilityEngine<T>
        where T : IComparable<T>
    {
        public void Run(
            Action code,
            bool runFinalIteration = true,
            bool unifyOnConvergence = true,
            bool bailOnFirstReverseWhenIdle = false,
            IImprobabilityEngine<T> next = null)
        {
            next?.Run(code, runFinalIteration, unifyOnConvergence, bailOnFirstReverseWhenIdle);
        }
    }
}
