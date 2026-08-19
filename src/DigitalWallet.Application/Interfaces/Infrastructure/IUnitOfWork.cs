namespace DigitalWallet.Application.Interfaces.Infrastructure;

// all or nothing. Application cant reference DbContext, so IUnitOfWork is used to save changes in a single atomic operation.
// If any of the changes fail, the entire operation is rolled back.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // Drops every pending change. For the case where an operation failed partway
    // through and the caller still needs to write something — a failed transfer
    // row — without persisting the mutations that were rejected.
    void Discard();
}