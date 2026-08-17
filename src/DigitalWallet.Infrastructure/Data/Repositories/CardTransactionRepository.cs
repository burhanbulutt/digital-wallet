using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class CardTransactionRepository : ICardTransactionRepository
{
    private readonly AppDbContext _context;

    public CardTransactionRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(CardTransaction transaction, CancellationToken ct = default)
        => await _context.CardTransactions.AddAsync(transaction, ct);

    public async Task<PagedResult<TransactionDto>> GetPagedForCardAsync(
        Guid cardId, Guid cardHolderId, TransactionListFilter filter,
        PaginationQuery pagination, CancellationToken ct = default)
    {
        var query = _context.CardTransactions
            .AsNoTracking()
            .Where(t => t.CardId == cardId && t.Card.CardHolderId == cardHolderId);

        if (filter.From is not null)     query = query.Where(t => t.TransactionDate >= filter.From);
        if (filter.To is not null)       query = query.Where(t => t.TransactionDate <  filter.To);
        if (filter.Category is not null) query = query.Where(t => t.Category == filter.Category);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.Id)          // rows sharing a timestamp
                                        // can't repeat or vanish across pages
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(TransactionDto.Projection)
            .ToListAsync(ct);

        return new PagedResult<TransactionDto>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}
