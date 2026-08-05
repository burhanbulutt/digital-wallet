using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class ProcessLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string ProcessName { get; set; } = null!;
    public string Message { get; set; } = null!;
    public Guid? EntityId { get; set; }
}
