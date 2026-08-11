using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

// TODO: remove CardHolderId when you implement JWt.
public record CardRequestDto(
    Guid CardHolderId,
    CardType CardType,
    CardBrand Brand);