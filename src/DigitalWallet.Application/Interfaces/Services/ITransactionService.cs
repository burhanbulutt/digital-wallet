using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionDto> AddAsync(
        Guid cardId, Guid cardHolderId, string idempotencyKey,
        CreateTransactionRequest request, CancellationToken ct = default);
}
