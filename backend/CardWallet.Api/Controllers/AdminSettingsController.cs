using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = "AdminOnly")]
public class AdminSettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminSettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _context.SystemSettings
            .OrderBy(x => x.Key)
            .Select(x => new { x.Key, x.Value, x.Description, x.UpdatedAt, x.CreatedAt })
            .ToListAsync();

        return Ok(settings);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(string key, [FromBody] UpsertSettingRequest request)
    {
        key = key.Trim();
        if (string.IsNullOrWhiteSpace(key)) return BadRequest("Key cấu hình không hợp lệ.");

        var setting = await _context.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SystemSettings.AddAsync(setting);
        }

        setting.Value = request.Value?.Trim() ?? string.Empty;
        setting.Description = request.Description?.Trim();
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { setting.Key, setting.Value, setting.Description, setting.UpdatedAt });
    }
}

public class UpsertSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
