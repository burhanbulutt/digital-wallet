using DigitalWallet.Application.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class CardHolderRepository : ICardHolderRepository
{
    private readonly AppDbContext _context;

    public CardHolderRepository(AppDbContext context) => _context = context;

    //temporary method(maybe).
    public async Task<bool> ExistsAsync(Guid cardHolderId, CancellationToken ct = default)
        => await _context.CardHolders
            .AsNoTracking()
            .AnyAsync(h => h.Id == cardHolderId, ct);
}