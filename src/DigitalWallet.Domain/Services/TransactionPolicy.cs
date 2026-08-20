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
        Card card, decimal amount, Category category, string? description,
        string? idempotencyKey, DateTimeOffset when)
    {
        CardPolicy.Spend(card, amount);

        return Record(card, amount, TransactionDirection.Outgoing,
                      category, description, idempotencyKey, when);
    }

    // incoming money to card
    public static CardTransaction RecordLoad(
        Card card, decimal amount, Category category, string? description,
        string? idempotencyKey, DateTimeOffset when)
    {
        CardPolicy.Load(card, amount);

        return Record(card, amount, TransactionDirection.Incoming,
                      category, description, idempotencyKey, when);
    }

    public static CardTransaction Failed(
        Guid cardId, decimal amount, TransactionDirection direction, Category category,
        string? description, string? idempotencyKey, string reason, DateTimeOffset when)
        => Unsuccessful(cardId, amount, direction, category, description,
                        idempotencyKey, TransactionStatus.Failed,
                        reason.Length > 200 ? reason[..200] : reason, when); // take the first 200 character if longer

    public static CardTransaction Cancelled(
        Guid cardId, decimal amount, TransactionDirection direction, Category category,
        string? description, string idempotencyKey, DateTimeOffset when)
        => Unsuccessful(cardId, amount, direction, category, description,
                        idempotencyKey, TransactionStatus.Cancelled,
                        "The client disconnected before the transaction was committed.", when);

    private static CardTransaction Record(
        Card card, decimal amount, TransactionDirection direction,
        Category category, string? description, string? idempotencyKey, DateTimeOffset when)
        => new()
        {
            Card = card,            // tracked, so EF fills in CardId on save
            Amount = amount,        // always positive; Direction carries the sign
            Direction = direction,
            Status = TransactionStatus.Completed,
            Category = category,
            Description = description,
            IdempotencyKey = idempotencyKey,
            TransactionDate = when
        };

    private static CardTransaction Unsuccessful(
        Guid cardId, decimal amount, TransactionDirection direction, Category category,
        string? description, string? idempotencyKey, TransactionStatus status,
        string reason, DateTimeOffset when)
        => new()
        {
            CardId = cardId,
            Amount = amount,
            Direction = direction,
            Status = status,
            Category = category,
            Description = description,
            FailureReason = reason,
            IdempotencyKey = idempotencyKey,
            TransactionDate = when
        };
}
