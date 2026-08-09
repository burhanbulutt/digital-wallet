using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Interfaces;

public interface ICardGenerator
{
    (Card Card, string CardNumber) Generate(CardType cardType, CardBrand brand);
}