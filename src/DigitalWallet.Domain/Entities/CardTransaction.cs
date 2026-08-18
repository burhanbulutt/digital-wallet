using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

// POS activity only. Transfers live in their own table and write nothing here.
public class CardTransaction : BaseEntity
{
    public Guid CardId { get; set; }

    // Always positive. Direction carries the sign.
    public decimal Amount { get; set; }
    public TransactionDirection Direction { get; set; }

    public Category Category { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset TransactionDate { get; set; }

    public Card Card { get; set; } = null!;
}
