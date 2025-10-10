using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Variables
{
    /// <summary>
    /// Direction of time: +1 for forward, -1 for reverse, this time machine only has two gears.
    /// </summary>
    public interface IPositronicVariable
    {
        /// <summary>
        /// Unifies all timeline slices into a single disjunctive state,
        /// kind of like a group project where everyone finally agrees on something.
        /// </summary>
        int Converged();

        /// <summary>
        /// Melds every branching future into one reluctant consensus, like a committee that's given up.
        /// </summary>
        void UnifyAll();
    }
}
