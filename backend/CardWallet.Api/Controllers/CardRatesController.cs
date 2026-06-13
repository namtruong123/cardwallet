using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/card-rates")]
public class CardRatesController : ControllerBase
{
    private readonly ICardRateService _cardRateService;

    public CardRatesController(ICardRateService cardRateService)
    {
        _cardRateService = cardRateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRates()
    {
        var result = await _cardRateService.GetActiveRatesAsync();
        return Ok(result);
    }
}
