using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.Users;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "CanManageUsers")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? keyword = "", [FromQuery] string? status = "", [FromQuery] bool? phoneVerified = null, [FromQuery] bool? emailVerified = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminUserService.GetUsersAsync(keyword ?? "", status ?? "", phoneVerified, emailVerified, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var result = await _adminUserService.GetUserDetailAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
        {
            var result = await _adminUserService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUserDetail), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserRequest request)
        {
            await _adminUserService.UpdateUserAsync(id, request);
            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminUpdateUserStatusRequest request)
        {
            await _adminUserService.UpdateStatusAsync(id, request.Status, GetCurrentUserId());
            return NoContent();
        }

        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockUser(Guid id)
        {
            await _adminUserService.LockUserAsync(id, GetCurrentUserId());
            return NoContent();
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            await _adminUserService.UnlockUserAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/verify-phone")]
        public async Task<IActionResult> VerifyPhone(Guid id, [FromBody] AdminVerifyRequest req) { await _adminUserService.VerifyPhoneAsync(id, req.IsVerified); return NoContent(); }

        [HttpPost("{id}/verify-email")]
        public async Task<IActionResult> VerifyEmail(Guid id, [FromBody] AdminVerifyRequest req) { await _adminUserService.VerifyEmailAsync(id, req.IsVerified); return NoContent(); }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request) { await _adminUserService.ResetPasswordAsync(id, request.NewPassword); return NoContent(); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _adminUserService.SoftDeleteUserAsync(id, GetCurrentUserId());
            return NoContent();
        }
    }
}
