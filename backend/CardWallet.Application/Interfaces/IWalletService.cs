using CardWallet.Application.DTOs.Wallet;
using CardWallet.Domain.Enums;
using CardWallet.Application.DTOs.Wallets;

namespace CardWallet.Application.Interfaces;

public interface IWalletService
{
    Task CreateWalletForUserAsync(Guid userId);
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);
    Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize);
    Task<WalletTransactionDto> AdjustBalanceAsync(
        Guid userId,
        long amount,
        WalletTransactionType type,
        string description,
        string? referenceCode = null,
        string? idempotencyKey = null);

    Task<WalletTransactionDto> AdminTransferAsync(Guid targetUserId, long amount, string reason, Guid adminUserId);

    Task<WalletTransactionDto> DepositAsync(Guid userId, DepositRequest request);
    Task<WalletTransactionDto> WithdrawAsync(Guid userId, WithdrawRequest request);
}
