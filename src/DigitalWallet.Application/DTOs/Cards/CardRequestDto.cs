using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

// TODO: remove CardHolderId when you implement JWt.
public record CardRequestDto(
    Guid CardHolderId,
    CardType CardType,
    CardBrand Brand,

    // Virtual cards only: the credit card to draw the limit from.
    Guid? MainCardId = null,

    // Credit and virtual cards only. Null means "give me the maximum still
    // available", which is the common case.
    decimal? RequestedLimit = null);
