using System.ComponentModel.DataAnnotations;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/v1/cards/{cardId:guid}/transactions")]
public class CardTransactionsController : ControllerBase
{
    private const string KeyPattern = @"^[A-Za-z0-9_\-]{8,64}$";
    private const string KeyError =
        "Idempotency-Key must be 8-64 characters of letters, digits, hyphen or underscore.";

    private readonly ITransactionService _transactionService;

    private readonly IDebtPaymentService _debtPaymentService;

    public CardTransactionsController(ITransactionService transactionService, IDebtPaymentService debtPaymentService)
    {
        _transactionService = transactionService;
        _debtPaymentService = debtPaymentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        Guid cardId,
        [FromQuery] Guid cardHolderId,
        [FromHeader(Name = "Idempotency-Key")]
        [Required]
        [RegularExpression(KeyPattern, ErrorMessage = KeyError)]
        string idempotencyKey,
        [FromBody] CreateTransactionRequest request,
        CancellationToken ct)
    {
        var created = await _transactionService.AddAsync(
            cardId, cardHolderId, idempotencyKey, request, ct);

        return StatusCode(StatusCodes.Status201Created, created);
    }

    // Pays down this card's(virtual or credit) debt from one of the holder's debit cards.
    [HttpPost("payments")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PayDebt(
        Guid cardId,
        [FromQuery] Guid cardHolderId,
        [FromHeader(Name = "Idempotency-Key")]
        [Required]
        [RegularExpression(KeyPattern, ErrorMessage = KeyError)]
        string idempotencyKey,
        [FromBody] PayDebtRequest request,
        CancellationToken ct)
    {
        var created = await _debtPaymentService.PayDebtAsync(
            cardId, cardHolderId, idempotencyKey, request, ct);

        return StatusCode(StatusCodes.Status201Created, created);
    }

}
