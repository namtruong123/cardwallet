using CardWallet.Application.DTOs.CardExchange;

namespace CardWallet.Application.Interfaces;

public interface IParentCardApiClient
{
    Task<ParentCardApiResponse> SubmitCardAsync(string provider, int faceValue, string cardCode, string serial, string idempotencyKey);
    Task<ParentCardApiResponse> GetTransactionStatusAsync(string parentTransactionCode);
}
