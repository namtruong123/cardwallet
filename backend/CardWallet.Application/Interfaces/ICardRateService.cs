using CardWallet.Application.DTOs.CardRates;

namespace CardWallet.Application.Interfaces;

public interface ICardRateService
{
    Task<List<CardRateDto>> GetActiveRatesAsync();
    Task<CardRateDto> GetByIdAsync(Guid id);
    Task<CardRateDto> CreateAsync(CreateCardRateRequest request);
    Task<CardRateDto> UpdateAsync(Guid id, UpdateCardRateRequest request);
    Task DeleteAsync(Guid id);
}
