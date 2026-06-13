using CardWallet.Application.DTOs.Wallet;
using CardWallet.Application.DTOs.Wallets;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;

namespace CardWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;

    public WalletService(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task CreateWalletForUserAsync(Guid userId)
    {
        var existingWallet = await _walletRepository.GetByUserIdAsync(userId);

        if (existingWallet != null)
            return;

        var wallet = new Wallet
        {
            UserId = userId,
            Balance = 0,
            LockedBalance = 0,
            Currency = "XU"
        };

        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);

        if (wallet == null)
            throw new BadRequestException("Ví chưa được khởi tạo.");

        return new WalletBalanceDto
        {
            Balance = wallet.Balance,
            LockedBalance = wallet.LockedBalance,
            Currency = wallet.Currency
        };
    }

    public async Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize)
    {
        var transactions = await _walletRepository.GetTransactionsAsync(userId, page, pageSize);

        return transactions.Select(x => new WalletTransactionDto
        {
            Id = x.Id,
            Type = x.Type.ToString(),
            Status = x.Status.ToString(),
            Amount = x.Amount,
            BalanceBefore = x.BalanceBefore,
            BalanceAfter = x.BalanceAfter,
            Description = x.Description,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<WalletTransactionDto> AdjustBalanceAsync(
        Guid userId,
        long amount,
        WalletTransactionType type,
        string description,
        string? referenceCode = null,
        string? idempotencyKey = null)
    {
        if (amount == 0)
            throw new BadRequestException("Số tiền giao dịch không hợp lệ.");

        var wallet = await _walletRepository.GetByUserIdForUpdateAsync(userId);

        if (wallet == null)
            throw new BadRequestException("Ví chưa được khởi tạo.");

        var balanceBefore = wallet.Balance;
        var balanceAfter = wallet.Balance + amount;

        if (balanceAfter < 0)
            throw new BadRequestException("Số dư không đủ.");

        wallet.Balance = balanceAfter;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            Type = type,
            Status = WalletTransactionStatus.Completed,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Description = description,
            ReferenceCode = referenceCode,
            IdempotencyKey = idempotencyKey
        };

        await _walletRepository.AddTransactionAsync(transaction);
        await _walletRepository.SaveChangesAsync();

        return new WalletTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Amount = transaction.Amount,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<WalletTransactionDto> AdminTransferAsync(Guid targetUserId, long amount, string reason, Guid adminUserId)
    {
        var transferAmount = Math.Abs(amount);
        if (transferAmount <= 0)
            throw new BadRequestException("Số điểm chuyển phải lớn hơn 0.");

        if (targetUserId == adminUserId)
            throw new BadRequestException("Không thể tự chuyển điểm cho chính tài khoản admin.");

        var adminWallet = await _walletRepository.GetByUserIdForUpdateAsync(adminUserId);
        if (adminWallet == null)
            throw new BadRequestException("Ví admin chưa được khởi tạo.");

        var targetWallet = await _walletRepository.GetByUserIdForUpdateAsync(targetUserId);
        if (targetWallet == null)
            throw new BadRequestException("Ví người nhận chưa được khởi tạo.");

        if (adminWallet.Balance < transferAmount)
            throw new BadRequestException("Ví admin không đủ điểm để chuyển.");

        var referenceCode = $"ADMIN_TRANSFER:{Guid.NewGuid():N}";
        var description = string.IsNullOrWhiteSpace(reason)
            ? "Admin chuyển điểm"
            : reason.Trim();

        var adminBefore = adminWallet.Balance;
        adminWallet.Balance -= transferAmount;
        adminWallet.UpdatedAt = DateTime.UtcNow;

        var targetBefore = targetWallet.Balance;
        targetWallet.Balance += transferAmount;
        targetWallet.UpdatedAt = DateTime.UtcNow;

        var adminTransaction = new WalletTransaction
        {
            WalletId = adminWallet.Id,
            UserId = adminUserId,
            Type = WalletTransactionType.AdminTransferOut,
            Status = WalletTransactionStatus.Completed,
            Amount = -transferAmount,
            BalanceBefore = adminBefore,
            BalanceAfter = adminWallet.Balance,
            Description = $"Chuyển điểm cho user {targetUserId}. {description}",
            ReferenceCode = referenceCode,
            IdempotencyKey = referenceCode
        };

        var targetTransaction = new WalletTransaction
        {
            WalletId = targetWallet.Id,
            UserId = targetUserId,
            Type = WalletTransactionType.AdminTransferIn,
            Status = WalletTransactionStatus.Completed,
            Amount = transferAmount,
            BalanceBefore = targetBefore,
            BalanceAfter = targetWallet.Balance,
            Description = $"Nhận điểm từ admin {adminUserId}. {description}",
            ReferenceCode = referenceCode,
            IdempotencyKey = referenceCode
        };

        await _walletRepository.AddTransactionAsync(adminTransaction);
        await _walletRepository.AddTransactionAsync(targetTransaction);
        await _walletRepository.SaveChangesAsync();

        return new WalletTransactionDto
        {
            Id = targetTransaction.Id,
            Type = targetTransaction.Type.ToString(),
            Status = targetTransaction.Status.ToString(),
            Amount = targetTransaction.Amount,
            BalanceBefore = targetTransaction.BalanceBefore,
            BalanceAfter = targetTransaction.BalanceAfter,
            Description = targetTransaction.Description,
            CreatedAt = targetTransaction.CreatedAt
        };
    }

    public Task<WalletTransactionDto> DepositAsync(Guid userId, DepositRequest request)
    {
        return AdjustBalanceAsync(userId, request.Amount, WalletTransactionType.Deposit, request.Description ?? string.Empty, request.ReferenceId);
    }

    public Task<WalletTransactionDto> WithdrawAsync(Guid userId, WithdrawRequest request)
    {
        return AdjustBalanceAsync(userId, -Math.Abs(request.Amount), WalletTransactionType.Withdraw, request.Description ?? string.Empty, request.ReferenceId);
    }
}
