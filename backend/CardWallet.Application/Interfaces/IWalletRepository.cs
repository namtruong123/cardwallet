using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId);
    Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId);
    Task AddAsync(Wallet wallet);
    Task AddTransactionAsync(WalletTransaction transaction);
    Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page, int pageSize);
    Task SaveChangesAsync();
}