namespace CardWallet.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = default!;

    public long Amount { get; set; } // Có thể âm (rút/trừ) hoặc dương (nạp/cộng)

    public string Type { get; set; } = string.Empty; // Deposit_Card, Withdraw, Commission...

    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Rejected

    public string? ReferenceId { get; set; } // ID của thẻ cào hoặc lệnh rút tiền

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}