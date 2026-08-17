namespace DigitalWallet.Application.DTOs.Budgets;

// not nullable since it is the only field(modifying the limit).
public record UpdateBudgetRequest(decimal LimitAmount);
