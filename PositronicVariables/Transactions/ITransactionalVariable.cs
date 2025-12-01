namespace PositronicVariables.Transactions
{
    public enum TxMutationKind
    {
        Append,
        ReplaceLast,
        OverwriteBootstrap
    }
    public interface ITransactionalVariable
    {
        long TxId { get; }
        long TxVersion { get; }
        object TxLock { get; }
        void TxApply(object qb, TxMutationKind kind);
        void TxBumpVersion();
    }
}
