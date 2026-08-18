namespace DigitalWallet.Application.DTOs.Transactions;

// Debt is paid from a debit card(source card). If that card has no money the holder tops it up first, same as
// real life.
public record PayDebtRequest(Guid SourceCardId, decimal Amount);
