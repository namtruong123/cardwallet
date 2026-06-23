using System;

namespace CardWallet.Domain.Entities;

public class PartnerTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long RewardCoins { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Scope { get; set; } = "Partner"; // System, Partner
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
