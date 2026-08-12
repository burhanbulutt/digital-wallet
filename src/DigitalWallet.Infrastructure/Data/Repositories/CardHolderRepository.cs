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

    // Salary is the only input CreditLimitPolicy needs, so project it rather
    // than loading the whole holder. Null means the holder does not exist,
    // which doubles as the existence check.
    public async Task<decimal?> GetSalaryAsync(Guid cardHolderId, CancellationToken ct = default)
        => await _context.CardHolders
            .AsNoTracking()
            .Where(h => h.Id == cardHolderId)
            .Select(h => (decimal?)h.Salary)
            .FirstOrDefaultAsync(ct);
}
