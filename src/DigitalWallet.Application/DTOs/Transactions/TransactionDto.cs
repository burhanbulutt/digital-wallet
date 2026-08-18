using System.Linq.Expressions;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Transactions;

public record TransactionDto(
    Guid Id,
    Guid CardId,
    decimal Amount,
    TransactionDirection Direction,
    Category Category,
    string? Description,
    DateTimeOffset TransactionDate)
{
    public static Expression<Func<CardTransaction, TransactionDto>> Projection => t =>
        new TransactionDto(t.Id, t.CardId, t.Amount, t.Direction,
                           t.Category, t.Description, t.TransactionDate);

    // Compiled once into a static field. One mapping definition, used as SQL by
    // the projection and as a delegate here.
    private static readonly Func<CardTransaction, TransactionDto> Compiled = Projection.Compile();

    public static TransactionDto From(CardTransaction transaction) => Compiled(transaction);
}
