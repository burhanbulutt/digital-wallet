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
    CardStatus Status,

    // Null for debit cards. For credit and virtual cards this is the limit that
    // was actually assigned, which may be lower than what was requested.
    decimal? LimitAmount,

    // Set only for virtual cards.
    Guid? MainCardId);
