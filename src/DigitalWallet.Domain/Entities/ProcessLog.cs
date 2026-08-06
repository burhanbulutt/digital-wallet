using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class ProcessLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public ProcessName ProcessName { get; set; }
    public string Message { get; set; } = null!;
    public Guid? EntityId { get; set; }
}
