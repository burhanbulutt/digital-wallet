using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

// Transfers live in their own table and write nothing here.
public class CardTransaction : BaseEntity
{
    public Guid CardId { get; set; }

    // Always positive. Direction carries the sign.
    public decimal Amount { get; set; }
    public TransactionDirection Direction { get; set; }
    public TransactionStatus Status { get; set; }

    public Category Category { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset TransactionDate { get; set; }

    // Populated only on Failed and Cancelled.
    public string? FailureReason { get; set; }

    // Client supplied, null for seeded rows. Unique per card rather than
    // globally: a debt payment writes two rows under one key, one per card.
    public string? IdempotencyKey { get; set; }

    public Card Card { get; set; } = null!;
}
