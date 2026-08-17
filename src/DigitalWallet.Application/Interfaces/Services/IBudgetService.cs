using DigitalWallet.Application.DTOs.Cards;

namespace DigitalWallet.Application.Interfaces.Services;

// Budgets are created with their card and read through it, so changing the
// limit is the only operation left.
public interface IBudgetService
{
    Task<CardDto> UpdateLimitAsync(
        Guid cardId, Guid cardHolderId, decimal newLimit, CancellationToken ct = default);
}
