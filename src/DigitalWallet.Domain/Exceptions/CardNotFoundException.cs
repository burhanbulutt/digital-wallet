namespace DigitalWallet.Domain.Exceptions;

public class CardNotFoundException : DomainException
{
    public CardNotFoundException(Guid EntityId)
        : base("Card not found.", EntityId) { }

    public CardNotFoundException(Guid EntityId, Guid cardId)
        : base($"Card with ID '{cardId}' was not found.", EntityId) { } // entity id for storing the cardholder id.
}
