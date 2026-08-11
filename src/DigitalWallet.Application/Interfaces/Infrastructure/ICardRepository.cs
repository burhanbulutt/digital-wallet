using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;
public interface ICardRepository
{
    Task AddAsync(Card card, CancellationToken ct = default);
    Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<int> CountActiveByHolderAsync(Guid cardHolderId, CancellationToken ct = default);
}