using CardWallet.Domain.Enums;

namespace CardWallet.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = default!;

    public Guid UserId { get; set; }

    public WalletTransactionType Type { get; set; }

    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

    // Số tiền thay đổi: cộng là dương, trừ là âm
    public long Amount { get; set; }
    public long BalanceBefore { get; set; }
    public long BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}