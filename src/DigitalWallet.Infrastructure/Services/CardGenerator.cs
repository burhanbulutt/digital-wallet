using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace DigitalWallet.Infrastructure.Services;

public class CardGenerator : ICardGenerator
{
    private readonly TimeProvider _timeProvider;
    private readonly byte[] _pepper;

    public CardGenerator(string pepper, TimeProvider timeProvider)
    {
        _pepper = Convert.FromBase64String(pepper);
        _timeProvider = timeProvider;
    }

    // CardHolderId, MainCardId and the Budget allocation are all the service's
    // job. This only produces the artifact.
    public (Card Card, string CardNumber) Generate(CardType cardType, CardBrand brand)
    {
        var cardNumber = GenerateCardNumber(brand);
        var (Month, Year) = GenerateExpiry();
        var card = new Card
        {
            CardNumberHash = HashCardNumber(cardNumber),
            CardType = cardType,
            Brand = brand,
            ExpiryMonth = Month,
            ExpiryYear = Year,
            Last4 = cardNumber.Substring(cardNumber.Length - 4),
            Balance = 0,
            Status = CardStatus.Active
        };

        return (card, cardNumber);
    }

    private string GenerateCardNumber(CardBrand brand)
    {
        string number = GetBinPrefix(brand);

        while (number.Length < 15)
        {
            number += RandomNumberGenerator.GetInt32(0, 10).ToString(); // more secure than System.Random. unpredictable.
        }

        number += CreateRightMostDigit(number);

        return number;
    }

    // Luhn algorithm to calculate the rightmost digit
    private static string CreateRightMostDigit(string number)
    {
        int nDigits = number.Length;

        int sum = 0;
        bool isSecond = true;
        for (int i = nDigits - 1; i >= 0; i--)
        {
             int d = number[i] - '0';

            if (isSecond == true){
                d = d * 2;
            }

            sum += d / 10;
            sum += d % 10;

            isSecond = !isSecond;
        }

        return ((10 - (sum % 10)) % 10).ToString();
    }

    private static string GetBinPrefix(CardBrand brand)
    {
        return brand switch
        {
            CardBrand.Visa       => "424242",
            CardBrand.Mastercard => "555555",
            CardBrand.Discover   => "601100",
            CardBrand.JCB        => "352800",
            _ => throw new ArgumentException($"Unsupported card brand: {brand}", nameof(brand))
        };
    }

    private (int Month, int Year) GenerateExpiry()
    {
        var now = _timeProvider.GetUtcNow();
        return (now.Month, now.Year + 3);
    }

    // If I wanna use hashing in application layer, I will create ICardNumberHasher
    private string HashCardNumber(string cardNumber)
    {
        return Convert.ToHexString(
            HMACSHA256.HashData(_pepper, Encoding.UTF8.GetBytes(cardNumber))
        );
    }
}
