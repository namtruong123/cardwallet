using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;

namespace CardWallet.Application.Services;

public class CardExchangeService : ICardExchangeService
{
    private readonly ICardTransactionRepository _transactionRepository;
    private readonly ICardRateRepository _cardRateRepository;
    private readonly IParentCardApiClient _parentClient;
    private readonly IWalletService _walletService;

    public CardExchangeService(
        ICardTransactionRepository transactionRepository,
        ICardRateRepository cardRateRepository,
        IParentCardApiClient parentClient,
        IWalletService walletService)
    {
        _transactionRepository = transactionRepository;
        _cardRateRepository = cardRateRepository;
        _parentClient = parentClient;
        _walletService = walletService;
    }

    public async Task<CardTransactionDto> SubmitCardAsync(Guid userId, SubmitCardRequest request)
    {
        // Idempotency: if provided and exists, return existing
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey!);
            if (existing != null)
                return ToDto(existing);
        }

        if (!Enum.TryParse<CardProvider>(request.Provider, true, out var provider))
            throw new BadRequestException("Provider không hợp lệ.");

        var rate = await _cardRateRepository.GetByProviderAndFaceValueAsync(provider, request.FaceValue);
        if (rate == null)
            throw new BadRequestException("Không tìm thấy bảng giá cho nhà mạng/mệnh giá.");

        var expectedReceive = (int)Math.Floor(rate.FaceValue * (1 - rate.DiscountPercent / 100m));

        var transaction = new CardTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            FaceValue = request.FaceValue,
            DiscountPercent = rate.DiscountPercent,
            ExpectedReceiveAmount = expectedReceive,
            CardCode = request.CardCode,
            Serial = request.Serial,
            Status = CardTransactionStatus.Pending,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Do not call parent API here — worker will process it asynchronously.
        return ToDto(transaction);
    }

    public async Task<List<CardTransactionDto>> GetUserTransactionsAsync(Guid userId)
    {
        var list = await _transactionRepository.GetByUserAsync(userId);
        return list.Select(ToDto).ToList();
    }

    public async Task<CardTransactionDto?> GetByIdAsync(Guid id)
    {
        var tx = await _transactionRepository.GetByIdAsync(id);
        return tx == null ? null : ToDto(tx);
    }

    public async Task<CardTransactionDto> FinalizeTransactionAsync(Guid transactionId, ParentCardApiResponse parentResult)
    {
        var tx = await _transactionRepository.GetByIdAsync(transactionId);
        if (tx == null) throw new KeyNotFoundException("Transaction not found");

        if (tx.Status == CardTransactionStatus.Success)
            return ToDto(tx);

        if (parentResult.Success)
        {
            tx.ActualReceiveAmount = parentResult.ActualReceiveAmount;
            tx.ParentTransactionCode = parentResult.ParentTransactionCode;
            tx.Status = CardTransactionStatus.Success;
            tx.CompletedAt = DateTime.UtcNow;

            var xu = parentResult.ActualReceiveAmount / 1000;

            // idempotent wallet credit
            await _walletService.AdjustBalanceAsync(tx.UserId, xu, WalletTransactionType.CardExchange, "Nhận tiền từ thẻ cào (finalize)", referenceCode: tx.Id.ToString(), idempotencyKey: $"CARD-{tx.Id}");

            await _transactionRepository.SaveChangesAsync();
        }
        else
        {
            tx.Status = CardTransactionStatus.Failed;
            tx.FailureReason = parentResult.FailureReason;
            tx.ErrorMessage = parentResult.FailureReason;
            tx.CompletedAt = DateTime.UtcNow;
            await _transactionRepository.SaveChangesAsync();
        }

        return ToDto(tx);
    }

    private static CardTransactionDto ToDto(CardTransaction tx)
    {
        return new CardTransactionDto
        {
            Id = tx.Id,
            UserId = tx.UserId,
            Provider = tx.Provider.ToString(),
            FaceValue = tx.FaceValue,
            DiscountPercent = tx.DiscountPercent,
            ExpectedReceiveAmount = tx.ExpectedReceiveAmount,
            ActualReceiveAmount = tx.ActualReceiveAmount,
            CardCode = tx.CardCode,
            Serial = tx.Serial,
            Status = tx.Status.ToString(),
            FailureReason = tx.FailureReason,
            IdempotencyKey = tx.IdempotencyKey,
            CreatedAt = tx.CreatedAt,
            CompletedAt = tx.CompletedAt
        };
    }
}
