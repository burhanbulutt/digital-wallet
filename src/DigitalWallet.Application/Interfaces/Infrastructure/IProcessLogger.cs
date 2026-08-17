using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface IProcessLogger
{
    Task LogAsync(
        ProcessName process,
        LogLevel level,
        string message,
        Guid? entityId = null);
}