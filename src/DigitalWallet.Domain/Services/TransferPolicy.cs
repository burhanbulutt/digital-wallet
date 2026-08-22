using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Domain.Services;

// Debit to debit only.
public static class TransferPolicy
{
    // Everything is checked before anything moves, so a rejection can never leave
    // a half applied balance in the change tracker.
    public static Transfer Execute(
        Card from, Card to, decimal amount, string idempotencyKey, DateTimeOffset when)
    {
        EnsureTransferable(from, to);

        CardPolicy.Spend(from, amount, DateOnly.FromDateTime(when.UtcDateTime));
        CardPolicy.Load(to, amount);

        return new Transfer
        {
            FromCard = from,   
            ToCard = to,
            Amount = amount,
            Status = TransferStatus.Completed,
            TransferDate = when,
            IdempotencyKey = idempotencyKey
        };
    }

    // Built from ids rather than entities: by the time this runs the change
    // tracker has been discarded because transfer failed, so the cards are no longer tracked.
    public static Transfer Failed(
        Guid fromCardId, Guid toCardId, decimal amount,
        string idempotencyKey, string reason, DateTimeOffset when)
        => new()
        {
            FromCardId = fromCardId,
            ToCardId = toCardId,
            Amount = amount,
            Status = TransferStatus.Failed,
            FailureReason = reason.Length > 200 ? reason[..200] : reason, // take the first 200 characters if longer
            TransferDate = when,
            IdempotencyKey = idempotencyKey
        };

    // The caller disconnected and the commit did not land, confirmed by the
    // absence of a row for this idempotency key. No money moved.
    public static Transfer Cancelled(
        Guid fromCardId, Guid toCardId, decimal amount,
        string idempotencyKey, DateTimeOffset when)
        => new()
        {
            FromCardId = fromCardId,
            ToCardId = toCardId,
            Amount = amount,
            Status = TransferStatus.Cancelled,
            FailureReason = "The client disconnected before the transfer was committed.",
            TransferDate = when,
            IdempotencyKey = idempotencyKey
        };

    private static void EnsureTransferable(Card from, Card to)
    {
        if (from.Id == to.Id)
            throw new InvalidTransferException(from.Id, "a card cannot transfer to itself.");

        EnsureHasBalance(from, "sender");
        EnsureHasBalance(to, "receiver");
    }

    private static void EnsureHasBalance(Card card, string role)
    {
        if (card.CardType is not (CardType.Debit or CardType.Prepaid))
            throw new InvalidTransferException(
                card.Id, $"the {role} must be a balance-backed card; this is a {card.CardType} card.");
    }
}
