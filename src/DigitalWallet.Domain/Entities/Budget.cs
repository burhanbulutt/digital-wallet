using DigitalWallet.Domain.Common;

namespace DigitalWallet.Domain.Entities;

// not a monthly plan.
public class Budget : BaseEntity
{
    public Guid CardId { get; set; }

    // user's choice of limit. it doesnt change depending on spending or reserving.
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }

    // Credit cards only. the sum of child virtual card limits.
    public decimal ReservedAmount { get; set; }

    // Prepaid cards only, null for everything else.
    public DateOnly? WindowStartDate { get; set; }

    public bool WarningThreshold80 { get; set; }
    public bool WarningThreshold100 { get; set; }

    // for every spend and reserve
    public byte[] RowVersion { get; set; } = null!;

    public Card Card { get; set; } = null!;
}
