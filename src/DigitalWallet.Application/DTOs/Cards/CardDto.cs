using System.Linq.Expressions;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

public record CardDto(
    Guid Id,
    string MaskedCardNumber,
    CardType CardType,
    CardBrand Brand,
    CardStatus Status,
    int ExpiryMonth,
    int ExpiryYear,
    decimal? Balance,
    decimal? LimitAmount,
    decimal? AvailableAmount,
    Guid? MainCardId,
    DateTimeOffset CreatedAt)
{
    // used for select when querying for cards. this projects them into cardDto's so returned object(s) is CardDto
    public static Expression<Func<Card, CardDto>> Projection => card => new CardDto(
        card.Id,
        "**** **** **** " + card.Last4,
        card.CardType,
        card.Brand,
        card.Status,
        card.ExpiryMonth,
        card.ExpiryYear,
        card.CardType == CardType.Debit ? card.Balance : null,
        card.Budget != null ? card.Budget.LimitAmount : null,
        card.Budget != null
            ? card.Budget.LimitAmount - card.Budget.SpentAmount - card.Budget.ReservedAmount
            : null,
        card.MainCardId,
        card.CreatedAt);

    private static readonly Func<Card, CardDto> Compiled = Projection.Compile();

    public static CardDto From(Card card) => Compiled(card);
}