using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories;

public class CardTransactionRepository : ICardTransactionRepository
{
    private readonly AppDbContext _db;

    public CardTransactionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(CardTransaction transaction)
    {
        await _db.CardTransactions.AddAsync(transaction);
    }

    public async Task<CardTransaction?> GetByIdAsync(Guid id)
    {
        return await _db.CardTransactions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CardTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _db.CardTransactions.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
    }

    public async Task<List<CardTransaction>> GetByUserAsync(Guid userId)
    {
        return await _db.CardTransactions.Where(x => x.UserId == userId).ToListAsync();
    }

    public async Task<List<CardTransaction>> GetPendingAsync(int limit)
    {
        var now = DateTime.UtcNow;
        var cutoff = DateTime.UtcNow.AddMinutes(-2);

        return await _db.CardTransactions
            .Where(x => (
                    x.Status == CardWallet.Domain.Enums.CardTransactionStatus.Pending
                    || x.Status == CardWallet.Domain.Enums.CardTransactionStatus.NeedReconcile
                    || (x.Status == CardWallet.Domain.Enums.CardTransactionStatus.Processing && (x.NextRetryAt == null || x.NextRetryAt <= now))
                    || (x.Status == CardWallet.Domain.Enums.CardTransactionStatus.Processing && x.ProcessedAt.HasValue && x.ProcessedAt <= cutoff)
                )
                && (x.NextRetryAt == null || x.NextRetryAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
