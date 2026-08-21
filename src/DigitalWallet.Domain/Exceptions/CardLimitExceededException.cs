namespace DigitalWallet.Domain.Exceptions;

public class CardLimitExceededException : DomainException
{
    public int MaxActiveCards { get; }
    public CardLimitExceededException(Guid entityId, int maxActiveCards)
        : base($"Card creation failed: the maximum of {maxActiveCards} "
             + "active cards has already been reached.", entityId)
    {
        MaxActiveCards = maxActiveCards;
    }
}
