using System.Security.Claims;
using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/card-exchange")]
[Authorize]
public class CardExchangeController : ControllerBase
{
    private readonly ICardExchangeService _cardExchangeService;

    public CardExchangeController(ICardExchangeService cardExchangeService)
    {
        _cardExchangeService = cardExchangeService;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit(SubmitCardRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _cardExchangeService.SubmitCardAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("my-transactions")]
    public async Task<IActionResult> MyTransactions()
    {
        var userId = GetCurrentUserId();
        var list = await _cardExchangeService.GetUserTransactionsAsync(userId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var tx = await _cardExchangeService.GetByIdAsync(id);
        if (tx == null) return NotFound();
        return Ok(tx);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var tx = await _cardExchangeService.GetByIdAsync(id);
        if (tx == null) return NotFound();

        return Ok(new {
            id = tx.Id,
            status = tx.Status,
            expectedReceiveAmount = tx.ExpectedReceiveAmount,
            actualReceiveAmount = tx.ActualReceiveAmount,
            message = tx.Status == "Processing" ? "Thẻ đang được xử lý" : (tx.Status == "Pending" ? "Chờ xử lý" : tx.FailureReason ?? string.Empty)
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdString!);
    }
}
