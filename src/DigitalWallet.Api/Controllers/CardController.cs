using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/cards")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService) => _cardService = cardService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        throw new NotImplementedException("");
    }

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