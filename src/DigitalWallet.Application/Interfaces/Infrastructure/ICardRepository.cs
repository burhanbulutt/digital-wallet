using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.DTOs.Common;
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

    Task<PagedResult<CardDto>> GetPagedForHolderAsync(
        Guid cardHolderId, CardListFilter filter, PaginationQuery pagination,
        CancellationToken ct = default);
    
    Task<CardDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<Card?> GetTrackedForStatusChangeAsync(Guid id, CancellationToken ct = default);

    // Tracked, with the Budget, because credit and virtual cards spend from it.
    Task<Card?> GetTrackedForSpendAsync(Guid id, CancellationToken ct = default);

    //  Difference is no children included, only parent included if exists. 
    Task<Card?> GetTrackedForLimitChangeAsync(Guid id, CancellationToken ct = default);

    Task<(CardDto Dto, Guid OwnerId)?> GetDtoWithOwnerAsync(Guid id, CancellationToken ct = default);

}
