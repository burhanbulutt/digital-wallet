using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transfers;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransferService
{
    Task<TransferDto> CreateAsync(
        Guid fromCardId, Guid cardHolderId, string idempotencyKey,
        CreateTransferRequest request, CancellationToken ct = default);

}
