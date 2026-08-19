using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transfers;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly AppDbContext _context;

    public TransferRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Transfer transfer, CancellationToken ct = default)
        => await _context.Transfers.AddAsync(transfer, ct);

    public async Task<TransferDto?> GetByIdempotencyKeyAsync(
        string idempotencyKey, CancellationToken ct = default)
        => await _context.Transfers
            .AsNoTracking()
            .Where(t => t.IdempotencyKey == idempotencyKey)
            .Select(TransferDto.Projection)
            .FirstOrDefaultAsync(ct);

}
