using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Domain.Services;

// pure business rules, no dependencies.
// I didnt wanna do this in Application.Services because in that case services would call each other. same for BudgetPolicy.
// Purpose: To check if user can allocate specified credit card limit.
public static class CreditLimitPolicy
{
    public const decimal SalaryMultiplier = 4m;

    public static decimal MaxTotalLimit(decimal salary)
        => salary * SalaryMultiplier;

    // alreadyAllocated is the sum of limits of all credit cards.
    public static decimal AvailableToAllocate(decimal salary, decimal alreadyAllocated)
        => Math.Max(0m, MaxTotalLimit(salary) - alreadyAllocated);

    // A null request means "give me everything still available".
    public static decimal ResolveRequestedLimit(decimal? requested, decimal available)
    {
        if (available <= 0m)
            throw new CreditLimitExceededException(requested ?? 0m, available);

        var limit = requested ?? available;

        if (limit <= 0m)
            throw new InvalidAmountException(limit);

        if (limit > available)
            throw new CreditLimitExceededException(limit, available);

        return limit;
    }
}
