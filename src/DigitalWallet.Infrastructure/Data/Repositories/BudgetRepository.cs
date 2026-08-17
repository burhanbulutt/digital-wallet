using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _context;

    public BudgetRepository(AppDbContext context) => _context = context;
    
    public async Task<decimal> SumCreditLimitsByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default)
        => await _context.Budgets
            .AsNoTracking()
            .Where(b => b.Card.CardHolderId == cardHolderId
                     && b.Card.CardType == CardType.Credit
                     && b.Card.Status != CardStatus.Closed)
            .SumAsync(b => b.LimitAmount, ct);
}
