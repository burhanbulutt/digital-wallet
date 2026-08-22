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
    public static Expression<Func<Card, CardDto>> Projection(DateOnly today) => card => new CardDto(
        card.Id,
        "**** **** **** " + card.Last4,
        card.CardType,
        card.Brand,
        card.Status,
        card.ExpiryMonth,
        card.ExpiryYear,
        card.CardType == CardType.Debit || card.CardType == CardType.Prepaid
            ? card.Balance
            : null,
        card.Budget != null ? card.Budget.LimitAmount : null,
        card.Budget == null
            ? null
            : card.Budget.WindowStartDate != null && card.Budget.WindowStartDate != today
                ? card.Budget.LimitAmount
                : card.Budget.LimitAmount - card.Budget.SpentAmount - card.Budget.ReservedAmount,
        card.MainCardId,
        card.CreatedAt);

    public static CardDto From(Card card, DateOnly today) => Projection(today).Compile()(card);
}