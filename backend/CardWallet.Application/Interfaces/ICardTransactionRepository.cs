using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces;

public interface ICardTransactionRepository
{
    Task AddAsync(CardTransaction transaction);
    Task<CardTransaction?> GetByIdAsync(Guid id);
    Task<CardTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<CardTransaction>> GetByUserAsync(Guid userId);
    Task<List<CardTransaction>> GetPendingAsync(int limit);
    Task SaveChangesAsync();
}
