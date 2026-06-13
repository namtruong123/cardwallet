using CardWallet.Application.DTOs.CardRates;
using CardWallet.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CardWallet.Api.Controllers;

[ApiController]
[Route("api/admin/card-rates")]
[Authorize(Roles = "Admin")]
public class AdminCardRatesController : ControllerBase
{
    private readonly ICardRateService _cardRateService;
    private readonly IValidator<CreateCardRateRequest> _createValidator;
    private readonly IValidator<UpdateCardRateRequest> _updateValidator;

    public AdminCardRatesController(
        ICardRateService cardRateService,
        IValidator<CreateCardRateRequest> createValidator,
        IValidator<UpdateCardRateRequest> updateValidator)
    {
        _cardRateService = cardRateService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCardRateRequest request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ValidationProblem(ToModelState(validationResult));

        var result = await _cardRateService.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCardRateRequest request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ValidationProblem(ToModelState(validationResult));

        var result = await _cardRateService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _cardRateService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _cardRateService.GetByIdAsync(id);
        return Ok(result);
    }

    private static ModelStateDictionary ToModelState(ValidationResult validationResult)
    {
        var modelState = new ModelStateDictionary();

        foreach (var error in validationResult.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return modelState;
    }
}
