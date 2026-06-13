using CardWallet.Application.DTOs.CardExchange;

namespace CardWallet.Application.Interfaces;

public interface ICardExchangeService
{
    Task<CardTransactionDto> SubmitCardAsync(Guid userId, SubmitCardRequest request);
    Task<List<CardTransactionDto>> GetUserTransactionsAsync(Guid userId);
    Task<CardTransactionDto?> GetByIdAsync(Guid id);

    Task<CardTransactionDto> FinalizeTransactionAsync(Guid transactionId, ParentCardApiResponse parentResult);
}
