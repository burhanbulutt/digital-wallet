using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

// Debit card to debit card, inside this institution. Synchronous, so a persisted
// transfer is always Completed or Failed. Nothing is written to CardTransaction.
public class Transfer : BaseEntity
{
    public Guid FromCardId { get; set; }
    public Guid ToCardId { get; set; }
    public decimal Amount { get; set; }
    public TransferStatus Status { get; set; }
    public DateTimeOffset TransferDate { get; set; }
    public string? FailureReason { get; set; }

    // Client supplied. Unique.
    public string IdempotencyKey { get; set; } = null!;

    public Card FromCard { get; set; } = null!;
    public Card ToCard { get; set; } = null!;
}
