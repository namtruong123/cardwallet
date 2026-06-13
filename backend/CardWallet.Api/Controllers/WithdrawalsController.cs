using System.Security.Claims;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/withdrawals")]
[Authorize]
public class WithdrawalsController : ControllerBase
{
    private const long MinimumWithdrawPoints = 100;
    private const int MonthlyLimit = 2;
    private readonly AppDbContext _context;

    public WithdrawalsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        var items = await _context.WithdrawalRequests
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.Amount,
                x.BankName,
                x.BankAccountNumber,
                x.BankAccountName,
                x.Status,
                x.RejectReason,
                x.CreatedAt,
                x.ReviewedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWithdrawalRequest request)
    {
        var userId = GetCurrentUserId();
        var amount = Math.Abs(request.Amount);

        if (amount < MinimumWithdrawPoints)
            return BadRequest("Số điểm rút tối thiểu là 100 điểm, tương ứng 100.000 VND.");

        if (string.IsNullOrWhiteSpace(request.BankName)
            || string.IsNullOrWhiteSpace(request.BankAccountNumber)
            || string.IsNullOrWhiteSpace(request.BankAccountName))
            return BadRequest("Vui lòng nhập đầy đủ ngân hàng, số tài khoản và tên chủ tài khoản.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        if (!NamesMatch(user.FullName, request.BankAccountName))
            return BadRequest("Tên chủ tài khoản ngân hàng phải trùng với tên đăng ký tài khoản.");

        var hasApprovedKyc = await _context.KycRequests.AnyAsync(x => x.UserId == userId && x.Status == "Approved");
        if (!hasApprovedKyc)
            return BadRequest("Tài khoản phải hoàn thành KYC cấp 2 trước khi rút xu.");

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthCount = await _context.WithdrawalRequests.CountAsync(x =>
            x.UserId == userId
            && x.CreatedAt >= monthStart
            && x.Status != "Rejected");
        if (monthCount >= MonthlyLimit)
            return BadRequest("Mỗi tài khoản chỉ được tạo tối đa 2 đơn rút trong tháng.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.UserId == userId);
        if (wallet == null) return BadRequest("Ví chưa được khởi tạo.");
        if (wallet.Balance < amount) return BadRequest("Số dư ví không đủ.");

        wallet.Balance -= amount;
        wallet.LockedBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var withdrawal = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            BankName = request.BankName.Trim(),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            BankAccountName = request.BankAccountName.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _context.WithdrawalRequests.AddAsync(withdrawal);
        await _context.SaveChangesAsync();

        return Ok(new { withdrawal.Id, withdrawal.Status, withdrawal.Amount, withdrawal.CreatedAt });
    }

    private static bool NamesMatch(string registeredName, string bankName)
    {
        return NormalizeName(registeredName) == NormalizeName(bankName);
    }

    private static string NormalizeName(string value)
    {
        return string.Join(' ', value.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }
}

public class CreateWithdrawalRequest
{
    public long Amount { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
}
