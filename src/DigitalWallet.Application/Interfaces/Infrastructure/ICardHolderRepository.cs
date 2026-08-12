namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ICardHolderRepository
{
    // not used anymore, GetSalaryAsync also checks for existence. Maybe should be used for clarity of task.
    Task<bool> ExistsAsync(Guid cardHolderId, CancellationToken ct = default); 
    Task<decimal?> GetSalaryAsync(Guid cardHolderId, CancellationToken ct = default);
}
