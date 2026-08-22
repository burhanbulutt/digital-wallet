using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CardRepository(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

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
            .FirstOrDefaultAsync(c => c.Id == id && c.CardHolderId == cardHolderId
                                     && c.Status != CardStatus.Closed, ct);

    public async Task<int> CountActiveByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking() // read only, no need to track for changes. memory save and faster execution.
            .CountAsync(c => c.CardHolderId == cardHolderId
                          && c.Status == CardStatus.Active, ct);


    public async Task<PagedResult<CardDto>> GetPagedForHolderAsync(
    Guid cardHolderId, CardListFilter filter, PaginationQuery pagination,
    CancellationToken ct = default)
    {
        var query = _context.Cards
            .AsNoTracking()
            .Where(c => c.CardHolderId == cardHolderId);

        if (filter.Status is not null)   query = query.Where(c => c.Status == filter.Status);
        if (filter.CardType is not null) query = query.Where(c => c.CardType == filter.CardType);
        if (filter.Brand is not null)    query = query.Where(c => c.Brand == filter.Brand);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)          // without this, rows sharing a
                                        // CreatedAt can repeat or vanish across pages
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(CardDto.Projection(Today))
            .ToListAsync(ct);

        return new PagedResult<CardDto>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<CardDto?> GetDtoByIdempotencyKeyAsync(
        string idempotencyKey, Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking()
            .Where(c => c.IdempotencyKey == idempotencyKey && c.CardHolderId == cardHolderId)
            .Select(CardDto.Projection(Today))
            .FirstOrDefaultAsync(ct);

    public async Task<CardDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CardDto.Projection(Today))
            .FirstOrDefaultAsync(ct);


    public async Task<Card?> GetTrackedForStatusChangeAsync(Guid id, CancellationToken ct = default)
        => await _context.Cards
            .Include(c => c.Budget)
            .Include(c => c.MainCard!).ThenInclude(p => p.Budget)
            .Include(c => c.VirtualCards).ThenInclude(v => v.Budget)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Card?> GetTrackedForTransactionAsync(Guid id, CancellationToken ct = default)
        => await _context.Cards
            .Include(c => c.Budget)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Card?> GetTrackedForLimitChangeAsync(Guid id, CancellationToken ct = default)
        => await _context.Cards
            .Include(c => c.Budget)
            .Include(c => c.MainCard!).ThenInclude(p => p.Budget)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(CardDto Dto, Guid OwnerId)?> GetDtoWithOwnerAsync(
        Guid id, CancellationToken ct = default)
    {
        var card = await _context.Cards
            .AsNoTracking()
            .Include(c => c.Budget)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    
        return card is null ? null : (CardDto.From(card, Today), card.CardHolderId);
    }
}
