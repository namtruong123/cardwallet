using CardWallet.Domain.Enums;

namespace CardWallet.Application.DTOs.CardRates;

public class UpdateCardRateRequest
{
    public CardProvider Provider { get; set; }
    public int FaceValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
