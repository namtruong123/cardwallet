namespace CardWallet.Application.DTOs.Wallets;

public class DepositRequest
{
    public long Amount { get; set; }
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
}
