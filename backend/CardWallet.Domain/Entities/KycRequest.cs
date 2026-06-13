namespace CardWallet.Domain.Entities;

public class KycRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string FrontIdImagePath { get; set; } = string.Empty;
    public string BackIdImagePath { get; set; } = string.Empty;
    public string SelfieImagePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? RejectReason { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
