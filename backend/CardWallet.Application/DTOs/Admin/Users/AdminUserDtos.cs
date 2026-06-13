using System;

namespace CardWallet.Application.DTOs.Admin.Users;

public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public Guid? ParentUserId { get; set; }
    public string? ParentFullName { get; set; }
    public string? ParentRole { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageTasks { get; set; }
    public bool CanApproveTasks { get; set; }
    public bool CanApproveKycWithdraw { get; set; }
    public bool CanTransferPoints { get; set; }
    public bool CanManageBlog { get; set; }
    public bool CanExportReports { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public decimal WalletBalance { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDetailDto : AdminUserListItemDto
{
    public string WalletId { get; set; } = string.Empty;
    public decimal LockedBalance { get; set; }
    public string KycStatus { get; set; } = "NotSubmitted";
}

public class AdminCreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
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
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
}

public class AdminUpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public Guid? ParentUserId { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageTasks { get; set; }
    public bool CanApproveTasks { get; set; }
    public bool CanApproveKycWithdraw { get; set; }
    public bool CanTransferPoints { get; set; }
    public bool CanManageBlog { get; set; }
    public bool CanExportReports { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
}

public class AdminUpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminVerifyRequest
{
    public bool IsVerified { get; set; }
}
