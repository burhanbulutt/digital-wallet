using DigitalWallet.Domain.Common;

namespace DigitalWallet.Domain.Entities;

public class Budget : BaseEntity
{
    public Guid CardId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public bool WarningThreshold80 { get; set; }
    public bool WarningThreshold100 { get; set; }

    public Card Card { get; set; } = null!;
}
