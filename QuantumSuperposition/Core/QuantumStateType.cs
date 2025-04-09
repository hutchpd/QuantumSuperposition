using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantumSuperposition.Core
{
    /// <summary>
    /// Represents the current mood of the QuBit. Is it feeling inclusive (All)?
    /// Or indecisive (Any)? Or has it finally made up its mind (Collapsed)?
    /// </summary>
    public enum QuantumStateType
    {
        SuperpositionAll,      // All states must be true. Like group projects, but successful.
        SuperpositionAny,      // Any state can be true. Like excuses for missing deadlines.
        CollapsedResult        // Only one state remains after collapse. R.I.P. potential.
    }
}
