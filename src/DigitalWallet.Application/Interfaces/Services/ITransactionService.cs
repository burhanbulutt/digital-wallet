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

    // Pays down a credit or virtual card's debt from one of the holder's debit
    // cards. Two cards, two transaction rows.
    Task<DebtPaymentDto> PayDebtAsync(
        Guid debtCardId, Guid cardHolderId, PayDebtRequest request,
        CancellationToken ct = default);
}
