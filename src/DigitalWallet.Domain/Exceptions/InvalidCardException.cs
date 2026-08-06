namespace DigitalWallet.Domain.Exceptions;

public class InvalidCardException : DomainException
{
    public InvalidCardException(Guid entityId)
        : base("The card is not valid for this operation.", entityId) { }

    public InvalidCardException(Guid entityId, string reason)
        : base($"Invalid card: {reason}", entityId) { }
}
