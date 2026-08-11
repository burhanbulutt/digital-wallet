namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ICardHolderRepository
{
    Task<bool> ExistsAsync(Guid cardHolderId, CancellationToken ct = default);
}