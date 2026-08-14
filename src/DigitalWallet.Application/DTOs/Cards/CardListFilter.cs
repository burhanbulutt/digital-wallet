// Application/DTOs/Cards/CardListFilter.cs
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

public record CardListFilter(
    CardStatus? Status = null,
    CardType? CardType = null,
    CardBrand? Brand = null);