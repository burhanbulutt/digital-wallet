using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/v1/cards")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService) => _cardService = cardService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] Guid cardHolderId,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] CardListFilter filter,
        CancellationToken ct)
        => Ok(await _cardService.GetPagedAsync(cardHolderId, filter, pagination, ct));


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id, [FromQuery] Guid cardHolderId, CancellationToken ct)
        => Ok(await _cardService.GetByIdAsync(id, cardHolderId, ct));


    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromQuery] Guid cardHolderId,
        [FromBody] UpdateCardStatusRequest request,
        CancellationToken ct)
        => Ok(await _cardService.UpdateStatusAsync(id, cardHolderId, request.Status, ct));


    /// Creates a virtual card. The response contains the full card number,
    /// which is returned here once and is not retrievable afterwards.
    [HttpPost]
    [ProducesResponseType(typeof(CardSecretsDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CardRequestDto request,
        CancellationToken ct)
    {
        var card = await _cardService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }
}