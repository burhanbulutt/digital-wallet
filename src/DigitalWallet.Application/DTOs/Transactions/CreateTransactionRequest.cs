using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Transactions;

public record CreateTransactionRequest(
    decimal Amount,
    Category Category,
    string? Description = null);
