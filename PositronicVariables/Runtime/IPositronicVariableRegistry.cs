using PositronicVariables.Variables;
using System.Collections.Generic;

namespace PositronicVariables.Runtime
{
    public interface IPositronicVariableRegistry : IEnumerable<IPositronicVariable>
    {
        void Add(IPositronicVariable variable);
        void Clear();
    }
}
