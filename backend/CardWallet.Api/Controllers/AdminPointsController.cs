using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers
{
    public class AdminTransferPointsRequest
    {
        public long Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/admin/users/{userId}/points")]
    [Authorize(Policy = "CanTransferPoints")]
    public class AdminPointsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public AdminPointsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private Guid GetCurrentAdminUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustPoints(Guid userId, [FromBody] AdminTransferPointsRequest request)
        {
            var adminUserId = GetCurrentAdminUserId();
            if (adminUserId == Guid.Empty) return Unauthorized();

            var result = await _walletService.AdminTransferAsync(userId, request.Amount, request.Reason, adminUserId);
            return Ok(result);
        }
    }
}
