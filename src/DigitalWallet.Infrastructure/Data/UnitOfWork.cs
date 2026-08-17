using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context){
        _context = context;
    } 

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();

            throw new ConcurrencyConflictException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // The failed entity is still tracked. Without this, the next
            // SaveChanges would retry the same doomed INSERT.
            _context.ChangeTracker.Clear();

            throw new DuplicateCardException();
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sql
           && sql.Number is UniqueIndexViolation or UniqueConstraintViolation;
}