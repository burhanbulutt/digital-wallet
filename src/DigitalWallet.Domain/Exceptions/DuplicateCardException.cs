namespace DigitalWallet.Domain.Exceptions;
public class DuplicateCardException : DomainException
{
    public DuplicateCardException(Guid entityId, int attempts)
        : base($"Could not allocate a unique card number after {attempts} attempts. "
             + "Please try again.", entityId) { }
}
