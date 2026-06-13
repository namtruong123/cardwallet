namespace CardWallet.Application.DTOs.Wallet;

public class AdminAdjustBalanceRequest
{
    public Guid UserId { get; set; }

    // Có thể âm hoặc dương
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}