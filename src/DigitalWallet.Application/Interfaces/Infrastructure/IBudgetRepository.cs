namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface IBudgetRepository
{
    // Total limit already allocated across the holder's CREDIT cards. Virtual
    // card limits are carved out of a parent, so they are excluded to avoid
    // double-counting against the salary ceiling. Closed cards release theirs.
    Task<decimal> SumCreditLimitsByHolderAsync(
        Guid cardHolderId, CancellationToken ct = default);
}
