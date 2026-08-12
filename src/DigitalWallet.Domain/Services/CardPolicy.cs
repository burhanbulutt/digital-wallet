using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Entities;
using  DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Services;

// Debit cards spend from Balance and implemented here. Credit and virtual cards
// spend from their allocation which implemented in BudgetPolicy.
public static class CardPolicy
{
    public const int MaxActiveCardsPerHolder = 5;

    public static void EnsureCanIssueCard(Guid cardHolderId, int activeCardCount)
    {
        if (activeCardCount >= MaxActiveCardsPerHolder)
            throw new CardLimitExceededException(cardHolderId, MaxActiveCardsPerHolder);
    }

    // Mirrors CHK_Card_MainCard: only a virtual card may have a parent, and it
    // must have one. Takes the raw values because the card does not exist yet.
    public static void EnsureMainCardShape(CardType cardType, Guid? mainCardId)
    {
        if (cardType == CardType.Virtual && mainCardId is null)
            throw new InvalidMainCardException("A virtual card requires a main card.");

        if (cardType != CardType.Virtual && mainCardId is not null)
            throw new InvalidMainCardException($"A {cardType} card cannot have a main card.");
    }

    // Every money movement starts with this. TransactionService and
    // TransferService both need it, which is why it is not written inline.
    public static void EnsureSpendable(Card card)
    {
        if (card.Status != CardStatus.Active)
            throw new InvalidCardException(
                card.Id, $"card is {card.Status}; only an Active card can be used.");
    }

    private static void EnsureDebitCard(Card card)
    {
        if (card.CardType != CardType.Debit)
            throw new InvalidCardException(
                card.Id,
                $"balance operations apply to debit cards only; this is a {card.CardType} card.");
    }

    public static void Withdraw(Card card, decimal amount)
    {
        EnsureDebitCard(card);

        if (amount <= 0m)
            throw new InvalidAmountException(card.Id, amount);

        if (amount > card.Balance)
            throw new InsufficientBalanceException(card.Id, amount, card.Balance);

        card.Balance -= amount;
    }

    public static void Deposit(Card card, decimal amount)
    {
        EnsureDebitCard(card);

        if (amount <= 0m)
            throw new InvalidAmountException(card.Id, amount);

        card.Balance += amount;
    }

    // Same status transitions are rejected, cant request to freeze an already frozen card.
    public static bool CanTransitionTo(CardStatus from, CardStatus to)
        => (from, to) switch
        {
            (CardStatus.Active, CardStatus.Frozen) => true,
            (CardStatus.Frozen, CardStatus.Active) => true,
            (CardStatus.Active, CardStatus.Closed) => true,
            (CardStatus.Frozen, CardStatus.Closed) => true,
            _ => false
        };

    public static void TransitionTo(Card card, CardStatus newStatus)
    {
        if (!CanTransitionTo(card.Status, newStatus))
            throw new InvalidCardException(
                card.Id, $"cannot change status from {card.Status} to {newStatus}.");

        card.Status = newStatus;
    }

}
