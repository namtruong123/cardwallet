namespace CardWallet.Application.DTOs.CardExchange;

public class ParentCardApiResponse
{
    public bool Success { get; set; }
    public int ActualReceiveAmount { get; set; }
    public string? ParentTransactionCode { get; set; }
    public string? FailureReason { get; set; }
}
