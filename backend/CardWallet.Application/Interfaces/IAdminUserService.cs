using System;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.Users;
using CardWallet.Application.DTOs.Common;

namespace CardWallet.Application.Interfaces
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string keyword, string status, bool? phoneVerified, bool? emailVerified, int page, int pageSize, Guid currentUserId);
        Task<AdminUserDetailDto> GetUserDetailAsync(Guid id, Guid currentUserId);
        Task<AdminUserDetailDto> CreateUserAsync(AdminCreateUserRequest request, Guid currentUserId);
        Task UpdateUserAsync(Guid id, AdminUpdateUserRequest request, Guid currentUserId);
        Task UpdateStatusAsync(Guid id, string status, Guid currentUserId);
        Task LockUserAsync(Guid id, Guid currentUserId);
        Task UnlockUserAsync(Guid id, Guid currentUserId);
        Task VerifyPhoneAsync(Guid id, bool isVerified, Guid currentUserId);
        Task VerifyEmailAsync(Guid id, bool isVerified, Guid currentUserId);
        Task ResetPasswordAsync(Guid id, string newPassword, Guid currentUserId);
        Task SoftDeleteUserAsync(Guid id, Guid currentUserId);
    }
}