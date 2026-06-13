using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _context;

    public WalletRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Wallet?> GetByUserIdAsync(Guid userId)
    {
        return _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId)
    {
        return _context.Wallets
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task AddAsync(Wallet wallet)
    {
        await _context.Wallets.AddAsync(wallet);
    }

    public async Task AddTransactionAsync(WalletTransaction transaction)
    {
        await _context.WalletTransactions.AddAsync(transaction);
    }

    public Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page, int pageSize)
    {
        return _context.WalletTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}