namespace DigitalWallet.Domain.Exceptions;

// A virtual card must hang off a credit card that is active and owned by the
// same holder. Anything else would let one holder draw on another's limit.
public class InvalidMainCardException : DomainException
{
    public InvalidMainCardException(string reason)
        : base($"Invalid main card for virtual card creation: {reason}") { }

    public InvalidMainCardException(Guid entityId, string reason)
        : base($"Invalid main card for virtual card creation: {reason}", entityId) { }
}
