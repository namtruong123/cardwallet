using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;

namespace CardWallet.Application.Interfaces;

public interface ICardRateRepository
{
    Task<List<CardRate>> GetActiveRatesAsync();
    Task<CardRate?> GetByIdAsync(Guid id);
    Task<CardRate?> GetByProviderAndFaceValueAsync(CardProvider provider, int faceValue);
    Task<bool> ExistsAsync(CardProvider provider, int faceValue, Guid? excludeId = null);
    Task AddAsync(CardRate cardRate);
    Task DeleteAsync(CardRate cardRate);
    Task SaveChangesAsync();
}
