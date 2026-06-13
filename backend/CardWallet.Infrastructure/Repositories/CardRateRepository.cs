using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories;

public class CardRateRepository : ICardRateRepository
{
    private readonly AppDbContext _context;

    public CardRateRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<CardRate>> GetActiveRatesAsync()
    {
        return _context.CardRates
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.FaceValue)
            .ToListAsync();
    }

    public Task<CardRate?> GetByIdAsync(Guid id)
    {
        return _context.CardRates
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<CardRate?> GetByProviderAndFaceValueAsync(CardProvider provider, int faceValue)
    {
        return _context.CardRates.FirstOrDefaultAsync(x => x.Provider == provider && x.FaceValue == faceValue && x.IsActive);
    }

    public Task<bool> ExistsAsync(CardProvider provider, int faceValue, Guid? excludeId = null)
    {
        return _context.CardRates
            .AnyAsync(x => x.Provider == provider
                && x.FaceValue == faceValue
                && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    public async Task AddAsync(CardRate cardRate)
    {
        await _context.CardRates.AddAsync(cardRate);
    }

    public Task DeleteAsync(CardRate cardRate)
    {
        _context.CardRates.Remove(cardRate);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
