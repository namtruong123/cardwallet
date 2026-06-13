namespace CardWallet.Application.DTOs.Admin;

public class AdminCardRateRequest
{
    public string Provider { get; set; } = string.Empty;
    public int FaceValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
