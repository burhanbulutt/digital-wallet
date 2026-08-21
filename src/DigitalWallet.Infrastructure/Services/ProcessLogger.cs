using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using LogLevel = DigitalWallet.Domain.Enums.LogLevel;

namespace DigitalWallet.Infrastructure.Services;

public class ProcessLogger : IProcessLogger
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessLogger> _logger;

    public ProcessLogger(
        IDbContextFactory<AppDbContext> factory,
        TimeProvider timeProvider,
        ILogger<ProcessLogger> logger)
    {
        _factory = factory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task LogAsync(
        ProcessName process,
        LogLevel level,
        string message,
        Guid? entityId = null)
    {
        try
        {
            await using var context = await _factory.CreateDbContextAsync(CancellationToken.None);

            context.ProcessLogs.Add(new ProcessLog
            {
                ProcessName = process,
                Level = level,
                Message = message,
                EntityId = entityId,
                Timestamp = _timeProvider.GetUtcNow()
            });

            await context.SaveChangesAsync(CancellationToken.None);
        }
        // when we use processlogger inside a catch, if the logAsync throws, it replaces the catched exception.
        // thus logAsync shouldnt throw.
        catch (Exception ex) 
        {
            _logger.LogError(ex,
                "Audit write failed; this ProcessLog row was lost. "
              + "Process={Process} Level={Level} EntityId={EntityId} Message={Message}",
                process, level, entityId, message);
        }
    }
}
