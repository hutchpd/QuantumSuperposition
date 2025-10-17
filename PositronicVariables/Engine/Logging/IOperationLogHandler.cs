namespace PositronicVariables.Engine.Logging
{
    public interface IOperationLogHandler<T>
    {
        void Record(IOperation op);
        void UndoLastForwardCycle();
        void Clear();
        bool SawForwardWrite { get; set; }
    }
}
