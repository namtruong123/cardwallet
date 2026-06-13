namespace CardWallet.Domain.Enums;

public enum WalletTransactionType
{
    Deposit = 1,
    Withdraw = 2,
    CardExchange = 3,
    Refund = 4,
    ReferralCommission = 5,
    AdminAdjustment = 6,
    AdminTransferOut = 7,
    AdminTransferIn = 8
}
