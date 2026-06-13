using CardWallet.Domain.Enums;

namespace CardWallet.Domain.Entities;

public class CardTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public CardProvider Provider { get; set; }
    public int FaceValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public int ExpectedReceiveAmount { get; set; }
    public int? ActualReceiveAmount { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public CardTransactionStatus Status { get; set; }
    public string? ParentTransactionCode { get; set; }
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ParentRequestRaw { get; set; }
    public string? ParentResponseRaw { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
