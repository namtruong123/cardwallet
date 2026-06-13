using System.Security.Claims;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Status,
            user.Role,
            user.ParentUserId,
            user.CanManageUsers,
            user.CanManageTasks,
            user.CanApproveTasks,
            user.CanApproveKycWithdraw,
            user.CanTransferPoints,
            user.CanManageBlog,
            user.CanExportReports,
            user.IsPhoneVerified,
            user.IsEmailVerified,
            user.CreatedAt
        });
    }
}
