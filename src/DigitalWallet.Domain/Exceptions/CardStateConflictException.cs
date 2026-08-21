namespace DigitalWallet.Domain.Exceptions;

public class CardStateConflictException : DomainException
{
    public CardStateConflictException(Guid entityId, string reason)
        : base($"Card state conflict: {reason}", entityId) { }
}
