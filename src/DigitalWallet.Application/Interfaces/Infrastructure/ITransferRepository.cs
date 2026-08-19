using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transfers;
using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ITransferRepository
{
    Task AddAsync(Transfer transfer, CancellationToken ct = default);

    // The idempotency check: a repeated request with the same key returns the
    // original transfer rather than moving money again.
    Task<TransferDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);

}
