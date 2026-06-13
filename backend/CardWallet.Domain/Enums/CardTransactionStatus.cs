namespace CardWallet.Domain.Enums;

public enum CardTransactionStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Cancelled,
    NeedReconcile
}
