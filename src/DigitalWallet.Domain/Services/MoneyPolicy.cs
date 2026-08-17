using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Domain.Services;

// One guard for every amount that moves. Was repeated in six places across
// CardPolicy and BudgetPolicy; the decimal-places rule only existed in none of them.
public static class MoneyPolicy
{
    public static void EnsureValid(Guid entityId, decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidAmountException(entityId, amount, "must be greater than zero.");

        // Rejected rather than rounded. Silent rounding is how money disappears.
        if (decimal.Round(amount, 2) != amount)
            throw new InvalidAmountException(entityId, amount, "may not have more than 2 decimal places.");
    }
}
