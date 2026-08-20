using System.ComponentModel.DataAnnotations;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transfers;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

// Routes are per action rather than per controller: creating a transfer is
// scoped to the sending card, listing them is scoped to the holder.
[ApiController]
[Route("api/v1/cards/{fromCardId:guid}/transfers")]
public class TransfersController : ControllerBase
{
    private const string KeyPattern = @"^[A-Za-z0-9_\-]{8,64}$";
    private const string KeyError =
        "Idempotency-Key must be 8-64 characters of letters, digits, hyphen or underscore.";
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
        => _transferService = transferService;

    /// Moves money from this card to another debit card. Requires an
    /// Idempotency-Key header: the client generates one UUID per intent and
    /// reuses it across retries.
    [HttpPost]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid fromCardId,
        [FromQuery] Guid cardHolderId,       
        [FromHeader(Name = "Idempotency-Key")] 
        [Required]
        [RegularExpression(KeyPattern, ErrorMessage = KeyError)]
        string idempotencyKey, // will come from UI when the confirmation screen renders
        [FromBody] CreateTransferRequest request,
        CancellationToken ct)
    {
        var transfer = await _transferService.CreateAsync(
            fromCardId, cardHolderId, idempotencyKey, request, ct);

        return StatusCode(StatusCodes.Status201Created, transfer);
    }
}
