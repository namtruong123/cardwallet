using System.Security.Claims;
using CardWallet.Application.DTOs.Wallets;
using CardWallet.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IValidator<DepositRequest> _depositValidator;
    private readonly IValidator<WithdrawRequest> _withdrawValidator;

    public WalletsController(IWalletService walletService, IValidator<DepositRequest> depositValidator, IValidator<WithdrawRequest> withdrawValidator)
    {
        _walletService = walletService;
        _depositValidator = depositValidator;
        _withdrawValidator = withdrawValidator;
    }

    [HttpGet("me/balance")]
    public async Task<IActionResult> GetMyBalance()
    {
        var userId = GetCurrentUserId();
        var result = await _walletService.GetBalanceAsync(userId);
        return Ok(result);
    }

    [HttpPost("deposit")]
    public IActionResult Deposit(DepositRequest request)
    {
        return BadRequest("Endpoint nạp ví trực tiếp đã bị khóa để bảo toàn tổng cung. Vui lòng dùng luồng đổi thẻ/nạp tiền đã xác thực hoặc tạo yêu cầu nạp để admin duyệt.");
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(WithdrawRequest request)
    {
        var validationResult = await _withdrawValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ValidationProblem(ToModelState(validationResult));

        var userId = GetCurrentUserId();
        var result = await _walletService.WithdrawAsync(userId, request);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }

    private static ModelStateDictionary ToModelState(ValidationResult validationResult)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validationResult.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        return modelState;
    }
}
