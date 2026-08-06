namespace DigitalWallet.Domain.Exceptions;

public class DuplicateCardException : DomainException
{
    // only for ProcessLog, wont be shown to user. card number generation is automatic anyway.
    public DuplicateCardException(Guid entityId, string cardNumber)
        : base($"A card with number '{cardNumber}' already exists.", entityId) { }
}
