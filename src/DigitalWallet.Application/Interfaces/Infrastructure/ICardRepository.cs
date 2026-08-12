using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;
public interface ICardRepository
{
    Task AddAsync(Card card, CancellationToken ct = default);
    Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // loads a card with scoping it to its owner.
    Task<Card?> GetByIdForHolderAsync(Guid id, Guid cardHolderId, CancellationToken ct = default);

    // Tracked. Used when the card is about to be mutated
    Task<Card?> GetTrackedByIdForHolderAsync(Guid id, Guid cardHolderId, CancellationToken ct = default);

    Task<int> CountActiveByHolderAsync(Guid cardHolderId, CancellationToken ct = default);
}
