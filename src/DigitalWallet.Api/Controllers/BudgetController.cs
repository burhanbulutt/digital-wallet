using DigitalWallet.Application.DTOs.Budgets;
using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/v1/cards/{cardId:guid}/budget")]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetController(IBudgetService budgetService) => _budgetService = budgetService;

    /// Changes a credit or virtual card's limit. Budgets are created with their
    /// card and read through it, so this is the only budget operation.
    /// For a virtual card this also adjusts the parent's reserved amount.
    [HttpPatch]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLimit(
        Guid cardId,
        [FromQuery] Guid cardHolderId,          
        [FromBody] UpdateBudgetRequest request,
        CancellationToken ct)
        => Ok(await _budgetService.UpdateLimitAsync(cardId, cardHolderId, request.LimitAmount, ct));
}
