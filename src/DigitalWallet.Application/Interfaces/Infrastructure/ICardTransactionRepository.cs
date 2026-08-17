using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ICardTransactionRepository
{
    Task AddAsync(CardTransaction transaction, CancellationToken ct = default);

    // Takes the holder id so ownership is a WHERE clause rather than a check
    // the caller could forget.
    Task<PagedResult<TransactionDto>> GetPagedForCardAsync(
        Guid cardId, Guid cardHolderId, TransactionListFilter filter,
        PaginationQuery pagination, CancellationToken ct = default);
}
