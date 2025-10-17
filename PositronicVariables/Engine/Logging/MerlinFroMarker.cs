namespace PositronicVariables.Engine.Logging
{
    /// <summary>
    /// Marks the beginning of a forward half‑cycle in the OperationLog.
    /// Lets reverse‑replay peel exactly one forward half‑cycle and no more.
    /// </summary>
    public sealed class MerlinFroMarker : IOperation
    {
        public string OperationName => "ForwardHalfCycleStart";
        public void Undo() { /* no‑op */ }
    }
}
