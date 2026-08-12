using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Domain.Services;

// Spend and reservation rules for a Budget. Stateless, takes the entity as a
// parameter so the entity stays a plain data holder. Both TransactionService
// and TransferService call this rather than each other.
public static class BudgetPolicy
{
    private const decimal WarningRatio80 = 0.80m;

    public static decimal Available(Budget budget)
        => budget.LimitAmount - budget.SpentAmount - budget.ReservedAmount;

    // for credit and virtual card spending.
    public static void Spend(Budget budget, decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidAmountException(budget.CardId, amount);

        var available = Available(budget);
        if (amount > available)
            throw new CreditLimitExceededException(budget.CardId, amount, available);

        budget.SpentAmount += amount;

        var ratio = budget.SpentAmount / budget.LimitAmount;
        if (ratio >= WarningRatio80) budget.WarningThreshold80 = true;
        if (ratio >= 1m) budget.WarningThreshold100 = true;
    }

    // A credit or virtual card is invalid without a Budget.
    public static Budget AllocateForCreditCard(Card card, decimal limit)
    {
        if (limit <= 0m)
            throw new InvalidAmountException(card.Id, limit);

        return Attach(card, limit);
    }

    public static Budget AllocateForVirtualCard(Card child, Budget parentBudget, decimal limit)
    {
        Reserve(parentBudget, limit);
        return Attach(child, limit);
    }

    // Both sides of the navigation, so adding the card alone also inserts the budget.
    private static Budget Attach(Card card, decimal limit)
    {
        var budget = new Budget { Card = card, LimitAmount = limit };
        card.Budget = budget;
        return budget;
    }

    // Parent side of a virtual card allocation.
    public static void Reserve(Budget parentBudget, decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidAmountException(parentBudget.CardId, amount);

        var available = Available(parentBudget);
        if (amount > available)
            throw new CreditLimitExceededException(parentBudget.CardId, amount, available);

        parentBudget.ReservedAmount += amount;
    }

    // Called when a virtual card is closed or its limit reduced.
    public static void Release(Budget parentBudget, decimal reduceAmount)
        => parentBudget.ReservedAmount -= reduceAmount;

    // Credit cards. A limit cannot drop below what is already committed
    // CHK_Budget_Capacity would reject it.
    public static void ChangeLimit(Budget budget, decimal newLimit)
    {
        if (newLimit <= 0m)
            throw new InvalidAmountException(budget.CardId, newLimit);

        var committed = budget.SpentAmount + budget.ReservedAmount;
        if (newLimit < committed)
            throw new CreditLimitExceededException(budget.CardId, committed, newLimit);

        budget.LimitAmount = newLimit;
    }

    // Virtual cards.
    public static void ChangeVirtualCardLimit(
        Budget childBudget, Budget parentBudget, decimal newLimit)
    {
        if (newLimit <= 0m)
            throw new InvalidAmountException(childBudget.CardId, newLimit);

        if (newLimit < childBudget.SpentAmount)
            throw new CreditLimitExceededException(
                childBudget.CardId, childBudget.SpentAmount, newLimit);

        var delta = newLimit - childBudget.LimitAmount;

        if (delta > 0m) Reserve(parentBudget, delta);
        else if (delta < 0m) Release(parentBudget, -delta);

        childBudget.LimitAmount = newLimit;
    }
}
