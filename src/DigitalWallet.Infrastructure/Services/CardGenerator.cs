using DigitalWallet.Application.Interfaces;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Infrastructure.Services;

public class CardGenerator : ICardGenerator
{
    public (Card Card, string CardNumber) Generate(CardType cardType, CardBrand brand)
    {
    }

    private string GenerateCardNumber(CardBrand brand)
    {
    }

    private static string GetBinPrefix(CardBrand brand)
    {        
    }

    private static (int Month, int Year) GenerateExpiry()
    {
    }

    private string HashCardNumber(string cardNumber)
    {
    }
}