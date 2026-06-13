namespace CardWallet.Application.DTOs.Wallet;

public class WalletBalanceDto
{
    public long Balance { get; set; }
    public long LockedBalance { get; set; }
    public long AvailableBalance => Balance - LockedBalance;
    public string Currency { get; set; } = "XU";
}