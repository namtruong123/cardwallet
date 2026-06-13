namespace CardWallet.Application.DTOs.Admin;

public class AdminWalletDto
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public long Balance { get; set; }
    public long LockedBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
