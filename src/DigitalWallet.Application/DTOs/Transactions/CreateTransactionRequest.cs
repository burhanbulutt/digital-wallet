using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Transactions;

// Amount is always positive. Direction says which way the money went:
// Outgoing for a purchase, Incoming for a refund.
public record CreateTransactionRequest(
    decimal Amount,
    TransactionDirection Direction,
    Category Category,
    string? Description = null);
