using DigitalWallet.Domain.Common;

namespace DigitalWallet.Domain.Entities;

public class CardHolder : BaseEntity
{
    public string CustomerNo { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Email { get; set; }
    public string FullName { get; set; } = null!;
    public decimal Salary { get; set; }

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
