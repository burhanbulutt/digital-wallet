using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class CardTransaction : BaseEntity
{
    public Guid CardId { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset TransactionDate { get; set; }

    public Card Card { get; set; } = null!;
}
