using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Controllers;

[ApiController]
[Route("api/v1/cards/{cardId:guid}/transactions")]
public class CardTransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public CardTransactionsController(ITransactionService transactionService)
        => _transactionService = transactionService;

    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        Guid cardId,
        [FromQuery] Guid cardHolderId,          //will come from the JWT
        [FromBody] CreateTransactionRequest request,
        CancellationToken ct)
    {
        var created = await _transactionService.AddAsync(cardId, cardHolderId, request, ct);
        return CreatedAtAction(nameof(GetPaged), new { cardId }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid cardId,
        [FromQuery] Guid cardHolderId,
        [FromQuery] TransactionListFilter filter,
        [FromQuery] PaginationQuery pagination,
        CancellationToken ct)
        => Ok(await _transactionService.GetPagedAsync(cardId, cardHolderId, filter, pagination, ct));
}
