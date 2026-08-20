using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class CardTransactionRepository : ICardTransactionRepository
{
    private readonly AppDbContext _context;

    public CardTransactionRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(CardTransaction transaction, CancellationToken ct = default)
        => await _context.CardTransactions.AddAsync(transaction, ct);

    public async Task<TransactionDto?> GetByIdempotencyKeyAsync(
        string idempotencyKey, Guid cardId, CancellationToken ct = default)
        => await _context.CardTransactions
            .AsNoTracking()
            .Where(t => t.IdempotencyKey == idempotencyKey && t.CardId == cardId)
            .Select(TransactionDto.Projection)
            .FirstOrDefaultAsync(ct);
}
