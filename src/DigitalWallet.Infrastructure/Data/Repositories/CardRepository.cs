using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _context;

    public CardRepository(AppDbContext context) => _context = context;

    //adds to the change tracker only, IUnitOfWork owns the commit.
    public async Task AddAsync(Card card, CancellationToken ct = default)
        => await _context.Cards.AddAsync(card, ct);

    public async Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    // Ownership is a WHERE clause, not a post-load if. A card belonging to
    // someone else simply is not returned, so the not-found path produces the
    // 404 we want and there is no separate check to forget.
    public async Task<Card?> GetByIdForHolderAsync(
        Guid id, Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.CardHolderId == cardHolderId, ct);

    // Tracked, with the Budget included, because the caller is about to mutate
    // it and needs the RowVersion for the concurrency check.
    public async Task<Card?> GetTrackedByIdForHolderAsync(
        Guid id, Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .Include(c => c.Budget)
            .FirstOrDefaultAsync(c => c.Id == id && c.CardHolderId == cardHolderId, ct);

    public async Task<int> CountActiveByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking() // read only, no need to track for changes. memory save and faster execution.
            .CountAsync(c => c.CardHolderId == cardHolderId
                          && c.Status == CardStatus.Active, ct);
}
