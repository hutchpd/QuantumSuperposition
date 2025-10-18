using System;

namespace PositronicVariables.Variables
{
    public partial class PositronicVariable<T>
        where T : IComparable<T>
    {
        // Assignment sugar using '|' has been removed.
        // Use Assign(value) for scalar assignment.
    }
}