using System.Security.Claims;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/admin/kyc")]
[Authorize(Policy = "CanApproveKycWithdraw")]
public class AdminKycController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminKycController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] string status = "Pending")
    {
        var query = _context.KycRequests.Include(x => x.User).AsQueryable();
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
                x.FrontIdImagePath,
                x.BackIdImagePath,
                x.SelfieImagePath,
                x.Status,
                x.RejectReason,
                x.CreatedAt,
                x.ReviewedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("requests/{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var request = await _context.KycRequests.FindAsync(id);
        if (request == null) return NotFound("Không tìm thấy hồ sơ KYC.");
        if (request.Status != "Pending") return BadRequest("Chỉ duyệt được hồ sơ đang chờ xử lý.");

        request.Status = "Approved";
        request.ReviewedByUserId = GetCurrentUserId();
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { request.Id, request.Status });
    }

    [HttpPost("requests/{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectKycRequest body)
    {
        var request = await _context.KycRequests.FindAsync(id);
        if (request == null) return NotFound("Không tìm thấy hồ sơ KYC.");
        if (request.Status != "Pending") return BadRequest("Chỉ từ chối được hồ sơ đang chờ xử lý.");

        request.Status = "Rejected";
        request.RejectReason = string.IsNullOrWhiteSpace(body.Reason) ? "Không đạt yêu cầu xác minh." : body.Reason.Trim();
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

public class RejectKycRequest
{
    public string Reason { get; set; } = string.Empty;
}
