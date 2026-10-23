using PositronicVariables.Variables;

namespace PositronicVariables.Engine.Logging
{
    public interface IOperation
    {
        /// <summary>
        /// Invokes the logic to undo the operation.
        /// </summary>
        void Undo();

        /// <summary>
        /// A polite label slapped on your operation so future archeologists of this code can blame someone else.
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// The positronic variable targeted by this operation, if applicable.
        /// </summary>
        IPositronicVariable TargetVariable => null;
    }
}
