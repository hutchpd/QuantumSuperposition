using PositronicVariables.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PositronicVariables.Runtime
{
    public interface IPositronicVariableRegistry : IEnumerable<IPositronicVariable>
    {
        void Add(IPositronicVariable variable);
        void Clear();
    }
}
