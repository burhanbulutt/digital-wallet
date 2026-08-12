using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Interfaces.Infrastructure;

public interface ICardGenerator
{
    // Produces the card artifact only: number, Luhn digit, expiry, hash.
    // Ownership is CardService's job; the Budget allocation is BudgetPolicy's,
    // because it also has to reserve against the parent and the generator
    // has no repositories to load one.
    (Card Card, string CardNumber) Generate(CardType cardType, CardBrand brand);
}
