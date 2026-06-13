namespace CardWallet.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    // Đơn vị xu. Ví dụ: 1 xu = 1.000 VND
    public long Balance { get; set; }

    public long LockedBalance { get; set; }

    public string Currency { get; set; } = "XU";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}