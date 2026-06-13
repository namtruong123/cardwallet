using CardWallet.Application.DTOs.CardRates;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;

namespace CardWallet.Application.Services;

public class CardRateService : ICardRateService
{
    private readonly ICardRateRepository _cardRateRepository;

    public CardRateService(ICardRateRepository cardRateRepository)
    {
        _cardRateRepository = cardRateRepository;
    }

    public async Task<List<CardRateDto>> GetActiveRatesAsync()
    {
        var rates = await _cardRateRepository.GetActiveRatesAsync();
        return rates.Select(ToDto).ToList();
    }

    public async Task<CardRateDto> CreateAsync(CreateCardRateRequest request)
    {
        if (await _cardRateRepository.ExistsAsync(request.Provider, request.FaceValue))
            throw new BadRequestException("Bảng giá thẻ với nhà mạng và mệnh giá này đã tồn tại.");

        var cardRate = new CardRate
        {
            Provider = request.Provider,
            FaceValue = request.FaceValue,
            DiscountPercent = request.DiscountPercent,
            IsActive = request.IsActive
        };

        await _cardRateRepository.AddAsync(cardRate);
        await _cardRateRepository.SaveChangesAsync();

        return ToDto(cardRate);
    }

    public async Task<CardRateDto> GetByIdAsync(Guid id)
    {
        var cardRate = await _cardRateRepository.GetByIdAsync(id);
        if (cardRate == null)
            throw new BadRequestException("Không tìm thấy bảng giá thẻ.");

        return ToDto(cardRate);
    }

    public async Task<CardRateDto> UpdateAsync(Guid id, UpdateCardRateRequest request)
    {
        var cardRate = await _cardRateRepository.GetByIdAsync(id);
        if (cardRate == null)
            throw new BadRequestException("Không tìm thấy bảng giá thẻ.");

        if (await _cardRateRepository.ExistsAsync(request.Provider, request.FaceValue, id))
            throw new BadRequestException("Bảng giá thẻ với nhà mạng và mệnh giá này đã tồn tại.");

        cardRate.Provider = request.Provider;
        cardRate.FaceValue = request.FaceValue;
        cardRate.DiscountPercent = request.DiscountPercent;
        cardRate.IsActive = request.IsActive;
        cardRate.UpdatedAt = DateTime.UtcNow;

        await _cardRateRepository.SaveChangesAsync();

        return ToDto(cardRate);
    }

    public async Task DeleteAsync(Guid id)
    {
        var cardRate = await _cardRateRepository.GetByIdAsync(id);
        if (cardRate == null)
            throw new BadRequestException("Không tìm thấy bảng giá thẻ.");

        await _cardRateRepository.DeleteAsync(cardRate);
        await _cardRateRepository.SaveChangesAsync();
    }

    private static CardRateDto ToDto(CardRate cardRate)
    {
        var receiveValue = (int)Math.Floor(cardRate.FaceValue * (1 - cardRate.DiscountPercent / 100m));

        return new CardRateDto
        {
            Id = cardRate.Id,
            Provider = cardRate.Provider.ToString(),
            FaceValue = cardRate.FaceValue,
            DiscountPercent = cardRate.DiscountPercent,
            ReceiveValue = receiveValue,
            IsActive = cardRate.IsActive
        };
    }
}
