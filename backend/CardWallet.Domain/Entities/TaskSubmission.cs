using System;

namespace CardWallet.Domain.Entities;

public class TaskSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public PartnerTask? Task { get; set; }
    public Guid CollaboratorId { get; set; }
    public User? Collaborator { get; set; }
    public string ProofUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? RejectReason { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
}
