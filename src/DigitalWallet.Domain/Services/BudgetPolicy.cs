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
        // Public, so it guards itself rather than trusting CardPolicy to have done it.
        MoneyPolicy.EnsureValid(budget.CardId, amount);

        var available = Available(budget);

        if (amount > available)
            throw new CreditLimitExceededException(budget.CardId, amount, available);

        budget.SpentAmount += amount;

        EvaluateThresholds(budget);
    }

    // Overpayment is rejected, debt of card is paid
    public static void Settle(Budget budget, decimal amount)
    {
        MoneyPolicy.EnsureValid(budget.CardId, amount);

        if (amount > budget.SpentAmount)
            throw new InvalidAmountException(
                budget.CardId, amount, $"exceeds the outstanding {budget.SpentAmount:N2}.");

        budget.SpentAmount -= amount;

        EvaluateThresholds(budget);
    }

    // Moving the virtual card's debt to it's parent(credit card) when closed.
    public static void MoveDebtOnClose(Budget childBudget, Budget parentBudget)
    {
        parentBudget.SpentAmount += childBudget.SpentAmount;
        childBudget.SpentAmount = 0m;

        Release(parentBudget, childBudget.LimitAmount);

        EvaluateThresholds(parentBudget);
    }

    // A credit or virtual card is invalid without a Budget.
    public static Budget AllocateForCreditCard(Card card, decimal limit)
    {
        MoneyPolicy.EnsureValid(card.Id, limit);

        return Attach(card, limit);
    }

    public static Budget AllocateForVirtualCard(Card child, Budget parentBudget, decimal limit)
    {
        Reserve(parentBudget, limit);
        return Attach(child, limit);
    }

    // Both sides of the navigation, so adding the card alone also inserts the budget.

    // Parent side of a virtual card allocation.
    public static void Reserve(Budget parentBudget, decimal amount)
    {
        MoneyPolicy.EnsureValid(parentBudget.CardId, amount);

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
        MoneyPolicy.EnsureValid(budget.CardId, newLimit);

        var committed = budget.SpentAmount + budget.ReservedAmount;
        if (newLimit < committed)
            throw new CreditLimitExceededException(budget.CardId, committed, newLimit);

        budget.LimitAmount = newLimit;
    }

    // Virtual cards.
    public static void ChangeVirtualCardLimit(
        Budget childBudget, Budget parentBudget, decimal newLimit)
    {
        MoneyPolicy.EnsureValid(childBudget.CardId, newLimit);

        if (newLimit < childBudget.SpentAmount)
            throw new CreditLimitExceededException(
                childBudget.CardId, childBudget.SpentAmount, newLimit);

        var delta = newLimit - childBudget.LimitAmount;

        if (delta > 0m) Reserve(parentBudget, delta);
        else if (delta < 0m) Release(parentBudget, -delta);

        childBudget.LimitAmount = newLimit;
    }

    private static Budget Attach(Card card, decimal limit)
    {
        var budget = new Budget { Card = card, LimitAmount = limit };
        card.Budget = budget;
        return budget;
    }

    private static void EvaluateThresholds(Budget budget)
    {
        var ratio = budget.SpentAmount / budget.LimitAmount;
        budget.WarningThreshold80 = ratio >= WarningRatio80;
        budget.WarningThreshold100 = ratio >= 1m;
    }
}
