using System.Security.Claims;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/admin/withdrawals")]
[Authorize(Policy = "CanApproveKycWithdraw")]
public class AdminWithdrawalsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminWithdrawalsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string status = "Pending")
    {
        var query = _context.WithdrawalRequests.Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == status);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                UserName = x.User.FullName,
                x.User.Email,
                x.User.PhoneNumber,
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

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var request = await _context.WithdrawalRequests.FindAsync(id);
        if (request == null) return NotFound("Không tìm thấy đơn rút xu.");
        if (request.Status != "Pending") return BadRequest("Chỉ duyệt được đơn đang chờ xử lý.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.UserId == request.UserId);
        if (wallet == null) return BadRequest("Ví người dùng không tồn tại.");
        if (wallet.LockedBalance < request.Amount) return BadRequest("Số dư khóa không đủ để tất toán đơn rút.");

        wallet.LockedBalance -= request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        request.Status = "Approved";
        request.ReviewedByUserId = GetCurrentUserId();
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { request.Id, request.Status });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectWithdrawalRequest body)
    {
        var request = await _context.WithdrawalRequests.FindAsync(id);
        if (request == null) return NotFound("Không tìm thấy đơn rút xu.");
        if (request.Status != "Pending") return BadRequest("Chỉ từ chối được đơn đang chờ xử lý.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.UserId == request.UserId);
        if (wallet == null) return BadRequest("Ví người dùng không tồn tại.");
        if (wallet.LockedBalance < request.Amount) return BadRequest("Số dư khóa không đủ để hoàn lại đơn rút.");

        wallet.LockedBalance -= request.Amount;
        wallet.Balance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        request.Status = "Rejected";
        request.RejectReason = string.IsNullOrWhiteSpace(body.Reason) ? "Đơn rút không đạt điều kiện duyệt." : body.Reason.Trim();
        request.ReviewedByUserId = GetCurrentUserId();
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { request.Id, request.Status, request.RejectReason });
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }
}

public class RejectWithdrawalRequest
{
    public string Reason { get; set; } = string.Empty;
}
