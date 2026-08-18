namespace DigitalWallet.Application.DTOs.Transactions;

// Both legs of the payment, so the caller sees the result on each card without
// re-fetching either.
public record DebtPaymentDto(
    Guid DebtCardId,
    Guid SourceCardId,
    decimal Amount,
    decimal RemainingDebt,
    decimal RemainingBalance,
    DateTimeOffset PaidAt);
