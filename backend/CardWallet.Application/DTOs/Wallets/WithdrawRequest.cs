namespace CardWallet.Application.DTOs.Wallets;

public class WithdrawRequest
{
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceId { get; set; }
}