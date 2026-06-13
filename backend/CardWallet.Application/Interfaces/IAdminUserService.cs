using System;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.Users;
using CardWallet.Application.DTOs.Common;

namespace CardWallet.Application.Interfaces
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string keyword, string status, bool? phoneVerified, bool? emailVerified, int page, int pageSize);
        Task<AdminUserDetailDto> GetUserDetailAsync(Guid id);
        Task<AdminUserDetailDto> CreateUserAsync(AdminCreateUserRequest request);
        Task UpdateUserAsync(Guid id, AdminUpdateUserRequest request);
        Task UpdateStatusAsync(Guid id, string status, Guid currentUserId);
        Task LockUserAsync(Guid id, Guid currentUserId);
        Task UnlockUserAsync(Guid id);
        Task VerifyPhoneAsync(Guid id, bool isVerified);
        Task VerifyEmailAsync(Guid id, bool isVerified);
        Task ResetPasswordAsync(Guid id, string newPassword);
        Task SoftDeleteUserAsync(Guid id, Guid currentUserId);
    }
}