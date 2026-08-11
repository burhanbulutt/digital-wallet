using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

// returned once, only after card creation. CardDto is returned for get requests.
public record CardSecretsDto(
    Guid Id,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    CardBrand Brand,
    CardType CardType,
    CardStatus Status);