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

    public async Task<int> CountActiveByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default)
        => await _context.Cards
            .AsNoTracking() // read only, no need to track for changes. memory save and faster execution.
            .CountAsync(c => c.CardHolderId == cardHolderId
                          && c.Status == CardStatus.Active, ct);
}