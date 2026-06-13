namespace CardWallet.Application.DTOs.Admin;

public class AdminAdjustWalletRequest
{
    public Guid UserId { get; set; }
    public long Amount { get; set; }
    public string Type { get; set; } = "deposit";
    public string? Description { get; set; }
}
