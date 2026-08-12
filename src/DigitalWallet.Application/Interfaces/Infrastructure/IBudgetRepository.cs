using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface IBudgetRepository
{
    Task AddAsync(Budget budget, CancellationToken ct = default);
    Task<Budget?> GetByCardIdAsync(Guid cardId, CancellationToken ct = default);
    Task<decimal> SumCreditLimitsByHolderAsync(Guid cardHolderId, CancellationToken ct = default);
}
