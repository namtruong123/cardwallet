namespace CardWallet.Application.DTOs.CardRates;

public class CardRateDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int FaceValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public int ReceiveValue { get; set; }
    public bool IsActive { get; set; }
}
