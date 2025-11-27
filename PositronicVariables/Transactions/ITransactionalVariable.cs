namespace PositronicVariables.Transactions
{
    public interface ITransactionalVariable
    {
        long TxId { get; }
        long TxVersion { get; }
        object TxLock { get; }
        void TxApplyRequired(object qb);
        void TxBumpVersion();
    }
}
