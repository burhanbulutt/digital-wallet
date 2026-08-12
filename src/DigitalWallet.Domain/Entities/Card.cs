using DigitalWallet.Domain.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class Card : BaseEntity
{
    public Guid CardHolderId { get; set; }
    public string CardNumberHash { get; set; } = null!;
    public string Last4{get; set; } = null!;
    public int ExpiryYear { get; set; }
    public int ExpiryMonth { get; set; }

    // Debit cards only. Always 0 for credit and virtual cards
    // check constraint for that in database: CHk_Card_Balance
    public decimal Balance { get; set; }

    public CardStatus Status { get; set; }
    public CardType CardType { get; set; }
    public CardBrand Brand { get; set; }

    // Virtual cards only
    // CHK_Card_MainCard
    public Guid? MainCardId { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public CardHolder CardHolder { get; set; } = null!;
    public Card? MainCard { get; set; }
    public ICollection<Card> VirtualCards { get; set; } = new List<Card>(); // credit cards only
    public ICollection<CardTransaction> Transactions { get; set; } = new List<CardTransaction>();

    // One budget per card. Null for debit cards.
    public Budget? Budget { get; set; }
}
