using System.ComponentModel.DataAnnotations;
using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/v1/cards")]
public class CardsController : ControllerBase
{
    private const string KeyPattern = @"^[A-Za-z0-9_\-]{8,64}$";
    private const string KeyError =
        "Idempotency-Key must be 8-64 characters of letters, digits, hyphen or underscore.";

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


    [HttpPost]
    [ProducesResponseType(typeof(CardSecretsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key")]
        [Required]
        [RegularExpression(KeyPattern, ErrorMessage = KeyError)]
        string idempotencyKey,
        [FromBody] CardRequestDto request,
        CancellationToken ct)
    {
        var result = await _cardService.CreateAsync(request, idempotencyKey, ct);

        return result.Existing is not null
            ? Ok(result.Existing)
            : CreatedAtAction(
                nameof(GetById), new { id = result.Created!.Id }, result.Created);
    }
}