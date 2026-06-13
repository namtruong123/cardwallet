using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Interfaces;

namespace CardWallet.Api.Clients;

public class ParentCardApiClient : CardWallet.Application.Interfaces.IParentCardApiClient
{
    // Mock implementation for initial development
    public Task<ParentCardApiResponse> SubmitCardAsync(string provider, int faceValue, string cardCode, string serial, string idempotencyKey)
    {
        // Example mock: Viettel 500000 -> actual 400000 success
        if (provider.Equals("Viettel", StringComparison.OrdinalIgnoreCase) && faceValue == 500000)
        {
            return Task.FromResult(new ParentCardApiResponse
            {
                Success = true,
                ActualReceiveAmount = 400000,
                ParentTransactionCode = $"PARENT-{Guid.NewGuid()}"
            });
        }

        // Default: failed
        return Task.FromResult(new ParentCardApiResponse
        {
            Success = false,
            ActualReceiveAmount = 0,
            FailureReason = "Mock: unsupported provider or face value"
        });
    }

    public Task<ParentCardApiResponse> GetTransactionStatusAsync(string parentTransactionCode)
    {
        // Mock: return success if code starts with PARENT
        if (!string.IsNullOrWhiteSpace(parentTransactionCode) && parentTransactionCode.StartsWith("PARENT-"))
        {
            return Task.FromResult(new ParentCardApiResponse
            {
                Success = true,
                ActualReceiveAmount = 400000,
                ParentTransactionCode = parentTransactionCode
            });
        }

        return Task.FromResult(new ParentCardApiResponse { Success = false, ActualReceiveAmount = 0, FailureReason = "Not found" });
    }
}
