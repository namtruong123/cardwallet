using CardWallet.Domain.Enums;

namespace CardWallet.Domain.Entities;

public class CardRate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CardProvider Provider { get; set; }

    public int FaceValue { get; set; }

    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
