using System.Collections.Generic;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phoneNumber);
    Task<User?> GetByEmailOrPhoneAsync(string login);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedUsersAsync(string keyword, string status, bool? phoneVerified, bool? emailVerified, int page, int pageSize);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByPhoneAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}
