using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ICardTransactionRepository
{
    Task AddAsync(CardTransaction transaction, CancellationToken ct = default);

    Task<TransactionDto?> GetByIdempotencyKeyAsync(
        string idempotencyKey, Guid cardId, CancellationToken ct = default);
}
