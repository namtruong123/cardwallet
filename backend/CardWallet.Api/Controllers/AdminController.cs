using CardWallet.Application.DTOs.Admin;
using CardWallet.Application.DTOs.CardRates;
using CardWallet.Application.DTOs.Wallets;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Enums;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWalletService _walletService;
    private readonly ICardRateService _cardRateService;

    public AdminController(
        AppDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IWalletService walletService,
        ICardRateService cardRateService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _walletService = walletService;
        _cardRateService = cardRateService;
    }

    [HttpGet("legacy/users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Status = u.Status,
                HasWallet = u.Wallet != null
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("legacy/users")]
    public async Task<IActionResult> CreateUser(AdminCreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Thông tin user không hợp lệ.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber))
        {
            return Conflict("Email hoặc số điện thoại đã tồn tại.");
        }

        var user = new Domain.Entities.User
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = "Active"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            HasWallet = false
        });
    }

    [HttpPut("legacy/users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, AdminUpdateUserStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Không tìm thấy người dùng.");

        user.Status = string.IsNullOrWhiteSpace(request.Status) ? user.Status : request.Status;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("wallets")]
    public async Task<IActionResult> GetWallets()
    {
        var wallets = await _context.Wallets
            .Include(w => w.User)
            .Select(w => new AdminWalletDto
            {
                WalletId = w.Id,
                UserId = w.UserId,
                UserName = w.User.FullName,
                Balance = w.Balance,
                LockedBalance = w.LockedBalance,
                Currency = w.Currency,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return Ok(wallets);
    }

    [HttpPost("wallets/adjust")]
    public IActionResult AdjustWallet(AdminAdjustWalletRequest request)
    {
        return BadRequest("Endpoint điều chỉnh ví trực tiếp đã bị khóa để bảo toàn tổng cung. Vui lòng dùng /api/admin/users/{userId}/points/adjust để chuyển điểm từ ví admin sang ví người nhận.");
    }

    [HttpGet("cardrates")]
    public async Task<IActionResult> GetCardRates()
    {
        var rates = await _cardRateService.GetActiveRatesAsync();
        return Ok(rates);
    }

    [HttpPost("cardrates")]
    public async Task<IActionResult> CreateCardRate(AdminCardRateRequest request)
    {
        if (!Enum.TryParse<CardProvider>(request.Provider, true, out var provider))
            return BadRequest("Nhà cung cấp không hợp lệ.");

        var created = await _cardRateService.CreateAsync(new CreateCardRateRequest
        {
            Provider = provider,
            FaceValue = request.FaceValue,
            DiscountPercent = request.DiscountPercent,
            IsActive = request.IsActive
        });

        return CreatedAtAction(nameof(GetCardRates), new { id = created.Id }, created);
    }

    [HttpPut("cardrates/{id}")]
    public async Task<IActionResult> UpdateCardRate(Guid id, AdminCardRateRequest request)
    {
        if (!Enum.TryParse<CardProvider>(request.Provider, true, out var provider))
            return BadRequest("Nhà cung cấp không hợp lệ.");

        var updated = await _cardRateService.UpdateAsync(id, new UpdateCardRateRequest
        {
            Provider = provider,
            FaceValue = request.FaceValue,
            DiscountPercent = request.DiscountPercent,
            IsActive = request.IsActive
        });

        return Ok(updated);
    }

    [HttpDelete("cardrates/{id}")]
    public async Task<IActionResult> DeleteCardRate(Guid id)
    {
        await _cardRateService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] string? userId, [FromQuery] string? walletId, [FromQuery] string? type, [FromQuery] string? status)
    {
        var query = _context.WalletTransactions
            .Include(t => t.Wallet)
            .ThenInclude(w => w.User)
            .AsQueryable();

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            query = query.Where(t => t.UserId == parsedUserId);
        }

        if (Guid.TryParse(walletId, out var parsedWalletId))
        {
            query = query.Where(t => t.WalletId == parsedWalletId);
        }

        if (!string.IsNullOrWhiteSpace(type) && type.Trim().ToLowerInvariant() != "all")
        {
            query = query.Where(t => t.Type.ToString().Equals(type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLowerInvariant() != "all")
        {
            query = query.Where(t => t.Status.ToString().Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .Select(t => new AdminTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                WalletId = t.WalletId,
                UserName = t.Wallet.User.FullName,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(transactions);
    }
}
