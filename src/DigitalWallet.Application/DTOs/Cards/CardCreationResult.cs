using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.DTOs.Cards;

// Exactly one of these is non-null. They cannot be a single type because the
// first response carries the PAN and a replay cannot: the number is generated
// once and never stored, so there is nothing to replay it from.
public record CardCreationResult(CardSecretsDto? Created, CardDto? Existing)
{
    public static CardCreationResult New(CardSecretsDto card) => new(card, null);

    public static CardCreationResult Replay(CardDto card) => new(null, card);
}
