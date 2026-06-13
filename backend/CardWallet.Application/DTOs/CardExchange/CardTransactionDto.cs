namespace CardWallet.Application.DTOs.CardExchange;

public class CardTransactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int FaceValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public int ExpectedReceiveAmount { get; set; }
    public int? ActualReceiveAmount { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
