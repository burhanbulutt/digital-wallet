using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class Transfer : BaseEntity
{
    public Guid FromCardId { get; set; }
    public Guid ToCardId { get; set; }
    public decimal Amount { get; set; }
    public TransferStatus Status { get; set; }
    public DateTimeOffset TransferDate { get; set; }

    public Card FromCard { get; set; } = null!;
    public Card ToCard { get; set; } = null!;
}
