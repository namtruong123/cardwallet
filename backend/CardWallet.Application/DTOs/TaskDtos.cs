using System;

namespace CardWallet.Application.DTOs.Task
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long RewardCoins { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Scope { get; set; } = "Partner";
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long RewardCoins { get; set; }
        public string Scope { get; set; } = "Partner";
        public DateTime? Deadline { get; set; }
    }

    public class TaskSubmissionDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public long TaskRewardCoins { get; set; }
        public Guid CollaboratorId { get; set; }
        public string CollaboratorName { get; set; } = string.Empty;
        public string ProofUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string Status { get; set; } = "Pending";
        public string? RejectReason { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class SubmitTaskRequest
    {
        public Guid TaskId { get; set; }
        public string ProofUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class ReviewSubmissionRequest
    {
        public string? RejectReason { get; set; }
    }
}
