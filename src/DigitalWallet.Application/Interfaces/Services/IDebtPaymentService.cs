using DigitalWallet.Application.DTOs.Transactions;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IDebtPaymentService
{
    // Pays down a credit or virtual card's debt from one of the holder's debit
    // cards. Two cards, two transaction rows, one atomic save.
    Task<TransactionDto> PayDebtAsync(
        Guid debtCardId, Guid cardHolderId, string idempotencyKey,
        PayDebtRequest request, CancellationToken ct = default);
}