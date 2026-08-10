using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Interfaces;

public interface IProcessLogger
{
    Task LogAsync(
        ProcessName process,
        LogLevel level,
        string message,
        Guid? entityId = null,
        CancellationToken ct = default);
}