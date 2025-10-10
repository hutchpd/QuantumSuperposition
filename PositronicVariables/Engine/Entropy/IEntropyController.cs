using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Engine.Entropy
{
    public interface IEntropyController
    {
        int Entropy { get; }

        /// <summary>
        /// In some universes time goes backwards, and in some it goes forwards. See Red Dwarf - backwards for details you smeghead.
        /// </summary>
        void Initialise();
        /// <summary>
        /// Flip the direction of time's arrow.
        /// </summary>
        void Flip();
    }
}
