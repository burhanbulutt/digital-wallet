using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Services;

// Completely separate from Transfer, which has its own table and
// writes nothing here.
public static class TransactionPolicy
{
    // The money moves and the record is created in one call, so a balance can
    // never change without a matching row.
    public static CardTransaction RecordSpend(
        Card card, decimal amount, Category category, string? description, DateTimeOffset when)
    {
        CardPolicy.Spend(card, amount);

        return Record(card, amount, TransactionDirection.Outgoing, category, description, when);
    }

    // incoming money to card
    public static CardTransaction RecordLoad(
        Card card, decimal amount, Category category, string? description, DateTimeOffset when)
    {
        CardPolicy.Load(card, amount);

        return Record(card, amount, TransactionDirection.Incoming, category, description, when);
    }

    private static CardTransaction Record(
        Card card, decimal amount, TransactionDirection direction,
        Category category, string? description, DateTimeOffset when)
        => new()
        {
            Card = card,            // tracked, so EF fills in CardId on save
            Amount = amount,        // always positive; Direction carries the sign
            Direction = direction,
            Category = category,
            Description = description,
            TransactionDate = when
        };
}
