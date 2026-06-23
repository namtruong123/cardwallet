using System.Collections.Generic;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _context.Users.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        return _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);
    }

    public Task<User?> GetByPhoneAsync(string phoneNumber)
    {
        var normalizedPhone = phoneNumber.Trim();
        return _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);
    }

    public Task<User?> GetByEmailOrPhoneAsync(string login)
    {
        var normalizedLogin = login.Trim();
        var normalizedEmail = normalizedLogin.ToLower();
        return _context.Users.FirstOrDefaultAsync(x =>
            x.Email.ToLower() == normalizedEmail || x.PhoneNumber == normalizedLogin);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedUsersAsync(string keyword, string status, bool? phoneVerified, bool? emailVerified, int page, int pageSize, Guid? parentUserId = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.FullName.Contains(keyword) || u.Email.Contains(keyword) || u.PhoneNumber.Contains(keyword));
        }

        if (parentUserId.HasValue)
        {
            query = query.Where(u => u.ParentUserId == parentUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        if (phoneVerified.HasValue)
        {
            query = query.Where(u => u.IsPhoneVerified == phoneVerified.Value);
        }

        if (emailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == emailVerified.Value);
        }

        var totalCount = await query.CountAsync();
        var users = await query.Include(u => u.Wallet)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, totalCount);
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        return _context.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail);
    }

    public Task<bool> ExistsByPhoneAsync(string phoneNumber)
    {
        return _context.Users.AnyAsync(x => x.PhoneNumber == phoneNumber);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
