using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionDto> AddAsync(
        Guid cardId, Guid cardHolderId, CreateTransactionRequest request,
        CancellationToken ct = default);

    Task<PagedResult<TransactionDto>> GetPagedAsync(
        Guid cardId, Guid cardHolderId, TransactionListFilter filter,
        PaginationQuery pagination, CancellationToken ct = default);
}
