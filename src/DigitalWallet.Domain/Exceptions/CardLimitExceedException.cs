namespace DigitalWallet.Domain.Exceptions;

public class CardLimitExceededException : Exception
{
    public Guid? CardHolderId { get; }
    public int MaxActiveCards { get; }

    public CardLimitExceededException(Guid? EntityId, int maxActiveCards)
        : base($"Card creation failed: holder '{EntityId}' already has the maximum number of active cards ({maxActiveCards}).")
    {
        CardHolderId = EntityId;
        MaxActiveCards = maxActiveCards;
    }
}