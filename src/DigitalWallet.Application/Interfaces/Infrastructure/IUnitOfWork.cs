namespace DigitalWallet.Application.Interfaces.Infrastructure;

// all or nothing. Application cant reference DbContext, so IUnitOfWork is used to save changes in a single atomic operation.
// If any of the changes fail, the entire operation is rolled back.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}