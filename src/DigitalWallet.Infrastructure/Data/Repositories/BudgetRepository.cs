using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _context;

    public BudgetRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Budget budget, CancellationToken ct = default)
        => await _context.Budgets.AddAsync(budget, ct);

    // Tracked on purpose — callers mutate SpentAmount / ReservedAmount and rely
    // on RowVersion being loaded for the optimistic concurrency check.
    public async Task<Budget?> GetByCardIdAsync(Guid cardId, CancellationToken ct = default)
        => await _context.Budgets.FirstOrDefaultAsync(b => b.CardId == cardId, ct);

    // Credit cards only. A virtual card's limit is carved out of its parent's
    // capacity, so including it would count the same credit twice against the
    // salary ceiling. Closed cards release their allocation.
    public async Task<decimal> SumCreditLimitsByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default)
        => await _context.Budgets
            .AsNoTracking()
            .Where(b => b.Card.CardHolderId == cardHolderId
                     && b.Card.CardType == CardType.Credit
                     && b.Card.Status != CardStatus.Closed)
            .SumAsync(b => b.LimitAmount, ct);
}
