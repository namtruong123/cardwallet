using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.Users;
using CardWallet.Application.DTOs.Common;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWalletRepository _walletRepository;
    private readonly ISearchAliasRepository _aliasRepository;

    public AdminUserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IWalletRepository walletRepository,
        ISearchAliasRepository aliasRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _walletRepository = walletRepository;
        _aliasRepository = aliasRepository;
    }

    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string keyword, string status, bool? phoneVerified, bool? emailVerified, int page, int pageSize)
    {
        var finalKeyword = keyword;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var alias = await _aliasRepository.GetByAliasAsync(keyword.ToLower().Trim(), "UserSearch");
            if (alias != null) finalKeyword = alias.Target;
        }

        var (users, totalCount) = await _userRepository.GetPagedUsersAsync(finalKeyword, status, phoneVerified, emailVerified, page, pageSize);
        var userList = users.ToList();
        var parentIds = userList
            .Where(u => u.ParentUserId.HasValue)
            .Select(u => u.ParentUserId!.Value)
            .Distinct()
            .ToList();
        var parents = await Task.WhenAll(parentIds.Select(id => _userRepository.GetByIdAsync(id)));
        var parentMap = parents
            .Where(p => p != null)
            .ToDictionary(p => p!.Id, p => p!);

        var items = userList.Select(u =>
        {
            User? parent = null;
            if (u.ParentUserId.HasValue)
                parentMap.TryGetValue(u.ParentUserId.Value, out parent);

            return new AdminUserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Status = u.Status,
                Role = u.Role,
                ParentUserId = u.ParentUserId,
                ParentFullName = parent?.FullName,
                ParentRole = parent?.Role,
                CanManageUsers = u.CanManageUsers,
                CanManageTasks = u.CanManageTasks,
                CanApproveTasks = u.CanApproveTasks,
                CanApproveKycWithdraw = u.CanApproveKycWithdraw,
                CanTransferPoints = u.CanTransferPoints,
                CanManageBlog = u.CanManageBlog,
                CanExportReports = u.CanExportReports,
                IsPhoneVerified = u.IsPhoneVerified,
                IsEmailVerified = u.IsEmailVerified,
                FailedLoginAttempts = u.FailedLoginAttempts,
                LockoutEndAt = u.LockoutEndAt,
                CreatedAt = u.CreatedAt,
                WalletBalance = u.Wallet?.Balance ?? 0
            };
        });

        return new PagedResult<AdminUserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailDto> GetUserDetailAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");

        var wallet = await _walletRepository.GetByUserIdAsync(id);
        User? parent = null;
        if (user.ParentUserId.HasValue)
            parent = await _userRepository.GetByIdAsync(user.ParentUserId.Value);

        return new AdminUserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            Role = user.Role,
            ParentUserId = user.ParentUserId,
            ParentFullName = parent?.FullName,
            ParentRole = parent?.Role,
            CanManageUsers = user.CanManageUsers,
            CanManageTasks = user.CanManageTasks,
            CanApproveTasks = user.CanApproveTasks,
            CanApproveKycWithdraw = user.CanApproveKycWithdraw,
            CanTransferPoints = user.CanTransferPoints,
            CanManageBlog = user.CanManageBlog,
            CanExportReports = user.CanExportReports,
            IsPhoneVerified = user.IsPhoneVerified,
            IsEmailVerified = user.IsEmailVerified,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockoutEndAt = user.LockoutEndAt,
            CreatedAt = user.CreatedAt,
            WalletId = wallet?.Id.ToString() ?? string.Empty,
            WalletBalance = wallet?.Balance ?? 0,
            LockedBalance = wallet?.LockedBalance ?? 0,
            KycStatus = "NotSubmitted"
        };
    }

    public async Task<AdminUserDetailDto> CreateUserAsync(AdminCreateUserRequest request)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phoneNumber = request.PhoneNumber.Trim();
        var password = request.Password;
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
        var role = NormalizeRole(request.Role);

        ValidateUserInput(fullName, email, phoneNumber, password, requirePassword: true);

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email đã tồn tại");
        if (await _userRepository.ExistsByPhoneAsync(phoneNumber))
            throw new ConflictException("Số điện thoại đã tồn tại");

        var permissions = ResolvePermissions(role, request);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = _passwordHasher.Hash(password),
            Status = status,
            Role = role,
            ParentUserId = request.ParentUserId,
            CanManageUsers = permissions.CanManageUsers,
            CanManageTasks = permissions.CanManageTasks,
            CanApproveTasks = permissions.CanApproveTasks,
            CanApproveKycWithdraw = permissions.CanApproveKycWithdraw,
            CanTransferPoints = permissions.CanTransferPoints,
            CanManageBlog = permissions.CanManageBlog,
            CanExportReports = permissions.CanExportReports,
            IsPhoneVerified = request.IsPhoneVerified,
            IsEmailVerified = request.IsEmailVerified,
            CreatedAt = DateTime.UtcNow
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Balance = 0,
            LockedBalance = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _walletRepository.AddAsync(wallet);
        await _userRepository.SaveChangesAsync();

        return await GetUserDetailAsync(user.Id);
    }

    public async Task UpdateUserAsync(Guid id, AdminUpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phoneNumber = request.PhoneNumber.Trim();
        var role = NormalizeRole(request.Role);

        ValidateUserInput(fullName, email, phoneNumber, password: string.Empty, requirePassword: false);

        var existingEmail = await _userRepository.GetByEmailAsync(email);
        if (existingEmail != null && existingEmail.Id != id) throw new ConflictException("Email thuộc về tài khoản khác");

        var existingPhone = await _userRepository.GetByPhoneAsync(phoneNumber);
        if (existingPhone != null && existingPhone.Id != id) throw new ConflictException("Số điện thoại thuộc về tài khoản khác");

        var permissions = ResolvePermissions(role, request);

        user.FullName = fullName;
        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.Status = string.IsNullOrWhiteSpace(request.Status) ? user.Status : request.Status.Trim();
        user.Role = role;
        user.ParentUserId = request.ParentUserId;
        user.CanManageUsers = permissions.CanManageUsers;
        user.CanManageTasks = permissions.CanManageTasks;
        user.CanApproveTasks = permissions.CanApproveTasks;
        user.CanApproveKycWithdraw = permissions.CanApproveKycWithdraw;
        user.CanTransferPoints = permissions.CanTransferPoints;
        user.CanManageBlog = permissions.CanManageBlog;
        user.CanExportReports = permissions.CanExportReports;
        user.IsPhoneVerified = request.IsPhoneVerified;
        user.IsEmailVerified = request.IsEmailVerified;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid currentUserId)
    {
        if (id == currentUserId) throw new BadRequestException("Không thể tự đổi trạng thái tài khoản của chính mình");
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");
        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    public async Task LockUserAsync(Guid id, Guid currentUserId)
    {
        if (id == currentUserId) throw new BadRequestException("Không thể tự khóa tài khoản của chính mình");
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");
        user.Status = "Locked";
        user.LockoutEndAt = DateTime.UtcNow.AddYears(100);
        await _userRepository.UpdateAsync(user);
    }

    public async Task UnlockUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");
        user.Status = "Active";
        user.LockoutEndAt = null;
        user.FailedLoginAttempts = 0;
        await _userRepository.UpdateAsync(user);
    }

    public async Task VerifyPhoneAsync(Guid id, bool isVerified)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            user.IsPhoneVerified = isVerified;
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task VerifyEmailAsync(Guid id, bool isVerified)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            user.IsEmailVerified = isVerified;
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new BadRequestException("Mật khẩu mới phải có tối thiểu 6 ký tự");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    public async Task SoftDeleteUserAsync(Guid id, Guid currentUserId)
    {
        if (id == currentUserId) throw new BadRequestException("Không thể tự vô hiệu hóa tài khoản của chính mình");
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException("Không tìm thấy người dùng");
        user.Status = "Deleted";
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    private static void ValidateUserInput(string fullName, string email, string phoneNumber, string password, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new BadRequestException("Họ tên không được để trống");
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new BadRequestException("Email không hợp lệ");
        if (!Regex.IsMatch(phoneNumber, @"^\d{9,11}$"))
            throw new BadRequestException("Số điện thoại phải có 9-11 chữ số");
        if (requirePassword && (string.IsNullOrWhiteSpace(password) || password.Length < 6))
            throw new BadRequestException("Mật khẩu phải có tối thiểu 6 ký tự");
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim() switch
        {
            "Admin" => "Admin",
            "CentralManager" => "CentralManager",
            "PartnerOrg" => "PartnerOrg",
            "Collaborator" => "Collaborator",
            _ => "Customer"
        };
    }

    private static PermissionPreset ResolvePermissions(string role, AdminCreateUserRequest request)
    {
        var preset = GetRolePreset(role);
        return new PermissionPreset(
            preset.CanManageUsers || request.CanManageUsers,
            preset.CanManageTasks || request.CanManageTasks,
            preset.CanApproveTasks || request.CanApproveTasks,
            preset.CanApproveKycWithdraw || request.CanApproveKycWithdraw,
            preset.CanTransferPoints || request.CanTransferPoints,
            preset.CanManageBlog || request.CanManageBlog,
            preset.CanExportReports || request.CanExportReports);
    }

    private static PermissionPreset ResolvePermissions(string role, AdminUpdateUserRequest request)
    {
        var preset = GetRolePreset(role);
        return new PermissionPreset(
            preset.CanManageUsers || request.CanManageUsers,
            preset.CanManageTasks || request.CanManageTasks,
            preset.CanApproveTasks || request.CanApproveTasks,
            preset.CanApproveKycWithdraw || request.CanApproveKycWithdraw,
            preset.CanTransferPoints || request.CanTransferPoints,
            preset.CanManageBlog || request.CanManageBlog,
            preset.CanExportReports || request.CanExportReports);
    }

    private static PermissionPreset GetRolePreset(string role)
    {
        return role switch
        {
            "Admin" => new PermissionPreset(true, true, true, true, true, true, true),
            "CentralManager" => new PermissionPreset(true, true, true, false, true, true, false),
            "PartnerOrg" => new PermissionPreset(true, true, true, false, true, true, false),
            _ => new PermissionPreset(false, false, false, false, false, false, false)
        };
    }

    private sealed record PermissionPreset(
        bool CanManageUsers,
        bool CanManageTasks,
        bool CanApproveTasks,
        bool CanApproveKycWithdraw,
        bool CanTransferPoints,
        bool CanManageBlog,
        bool CanExportReports);
}
