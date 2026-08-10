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
    public decimal Balance { get; set; }
    public CardStatus Status { get; set; }
    public CardType CardType { get; set; }
    public CardBrand Brand { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public CardHolder CardHolder { get; set; } = null!;
    public ICollection<CardTransaction> Transactions { get; set; } = new List<CardTransaction>();
    public ICollection<Budget>? Budgets { get; set; }
}
