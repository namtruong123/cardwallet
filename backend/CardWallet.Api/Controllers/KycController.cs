using System.Security.Claims;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public KycController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        var latest = await _context.KycRequests
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.RejectReason,
                x.FrontIdImagePath,
                x.BackIdImagePath,
                x.SelfieImagePath,
                x.CreatedAt,
                x.ReviewedAt
            })
            .FirstOrDefaultAsync();

        return Ok(latest);
    }

    [HttpPost("submit")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Submit([FromForm] IFormFile frontId, [FromForm] IFormFile backId, [FromForm] IFormFile selfie)
    {
        var userId = GetCurrentUserId();
        if (!IsValidImage(frontId) || !IsValidImage(backId) || !IsValidImage(selfie))
            return BadRequest("KYC bắt buộc upload ảnh CCCD mặt trước, mặt sau và ảnh khuôn mặt định dạng JPG/PNG/WEBP.");

        var hasPendingOrApproved = await _context.KycRequests.AnyAsync(x =>
            x.UserId == userId && (x.Status == "Pending" || x.Status == "Approved"));
        if (hasPendingOrApproved)
            return BadRequest("Tài khoản đã có hồ sơ KYC đang chờ duyệt hoặc đã được duyệt.");

        var request = new KycRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        request.FrontIdImagePath = await SaveKycFile(frontId, request.Id, "front");
        request.BackIdImagePath = await SaveKycFile(backId, request.Id, "back");
        request.SelfieImagePath = await SaveKycFile(selfie, request.Id, "selfie");

        await _context.KycRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        return Ok(new { request.Id, request.Status, request.CreatedAt });
    }

    private bool IsValidImage(IFormFile? file)
    {
        return file != null
            && file.Length > 0
            && file.Length <= 5_000_000
            && AllowedContentTypes.Contains(file.ContentType);
    }

    private async Task<string> SaveKycFile(IFormFile file, Guid requestId, string slot)
    {
        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "kyc", requestId.ToString("N"));
        Directory.CreateDirectory(uploadRoot);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var fileName = $"{slot}{ext}";
        var absolutePath = Path.Combine(uploadRoot, fileName);

        await using var stream = System.IO.File.Create(absolutePath);
        await file.CopyToAsync(stream);

        return $"/uploads/kyc/{requestId:N}/{fileName}";
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }
}
