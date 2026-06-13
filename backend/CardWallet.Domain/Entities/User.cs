namespace CardWallet.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsPhoneVerified { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public string Role { get; set; } = "Customer";

    public Guid? ParentUserId { get; set; }

    public bool CanManageUsers { get; set; }

    public bool CanManageTasks { get; set; }

    public bool CanApproveTasks { get; set; }

    public bool CanApproveKycWithdraw { get; set; }

    public bool CanTransferPoints { get; set; }

    public bool CanManageBlog { get; set; }

    public bool CanExportReports { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutEndAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public Wallet? Wallet { get; set; }
}
