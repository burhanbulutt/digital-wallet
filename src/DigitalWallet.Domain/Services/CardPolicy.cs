using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Entities;
using  DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Services;

namespace DigitalWallet.Domain.Services;

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
    // TransferService both need it.
    public static void EnsureSpendable(Card card)
    {
        if (card.Status != CardStatus.Active)
            throw new InvalidCardException(
                card.Id, $"card is {card.Status}; only an Active card can be used.");
    }

    public static void Close(Card card)
    {
        if (card.CardType == CardType.Credit)
        {
            EnsureCreditCardSettled(card);

            foreach (var child in card.VirtualCards.Where(v => v.Status != CardStatus.Closed))
            {
                TransitionTo(child, CardStatus.Closed);
                BudgetPolicy.MoveDebtOnClose(child.Budget!, card.Budget!);
            }
        }

        // No condition for debit card. It is closed regardlessly
        TransitionTo(card, CardStatus.Closed);

        if (card.CardType == CardType.Virtual)
        {
            var parentBudget = card.MainCard?.Budget
                ?? throw new InvalidMainCardException(card.Id, "main card budget was not loaded.");

            var childBudget = card.GetRequiredBudget();

            BudgetPolicy.MoveDebtOnClose(childBudget, parentBudget);
        }
    }

    // Call to spend for any card.
    public static void Spend(Card card, decimal amount)
    {
        // Inside Spend so no caller can move money off a frozen or closed card
        // by forgetting the check.
        EnsureSpendable(card);
        MoneyPolicy.EnsureValid(card.Id, amount);

        if(card.CardType == CardType.Debit)
        {
            if (amount > card.Balance)
                throw new InsufficientBalanceException(card.Id, amount, card.Balance);

            card.Balance -= amount;
        }
        else
        {
            var budget = card.GetRequiredBudget();

            BudgetPolicy.Spend(budget, amount);
        }
    }

    public static void Deposit(Card card, decimal amount)
    {
        EnsureDebitCard(card);
        MoneyPolicy.EnsureValid(card.Id, amount);

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

    private static void EnsureDebitCard(Card card)
    {
        if (card.CardType != CardType.Debit)
            throw new InvalidCardException(
                card.Id,
                $"balance operations apply to debit cards only; this is a {card.CardType} card.");

    }

    // If spentAmount for all children and parent is zero you can cascade the delete.
    private static void EnsureCreditCardSettled(Card card)
    {
        var budget = card.GetRequiredBudget();

        if (budget.SpentAmount > 0m)
            throw new InvalidCardException(
                card.Id,
                $"cannot close a card with {budget.SpentAmount:N2} outstanding. Settle it first.");

        foreach (var child in card.VirtualCards.Where(v => v.Status != CardStatus.Closed))
        {
            var childBudget = child.GetRequiredBudget();

            if (childBudget.SpentAmount > 0m)
                throw new InvalidCardException(
                    card.Id,
                    $"cannot close: virtual card ****{child.Last4} has "
                  + $"{childBudget.SpentAmount:N2} outstanding.");
        }
    }

    private static Budget GetRequiredBudget(this Card card)
    => card.Budget ?? throw new InvalidCardException(
        card.Id, $"{card.CardType} card budget was not found.");

}
