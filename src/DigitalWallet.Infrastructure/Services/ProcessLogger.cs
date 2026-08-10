using DigitalWallet.Application.Interfaces;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Services;

public class ProcessLogger : IProcessLogger
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProcessLogger(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task LogAsync(
        ProcessName process,
        LogLevel level,
        string message,
        Guid? entityId = null,
        CancellationToken ct = default)
    {
        await using var context = await _factory.CreateDbContextAsync(ct);// context is cleaned up asynchronously when it goes out of scope

        context.ProcessLogs.Add(new ProcessLog
        {
            ProcessName = process,
            Level = level,
            Message = message,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
    }
}