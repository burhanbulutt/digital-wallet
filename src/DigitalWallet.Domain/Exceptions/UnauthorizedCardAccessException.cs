namespace DigitalWallet.Domain.Exceptions;

public class UnauthorizedCardAccessException : DomainException
{

    // Those can be modified later on. Change it accordingly in the future.
    public UnauthorizedCardAccessException(Guid EntityId)
        : base("You are not authorized to access this card.", EntityId) { }

    public UnauthorizedCardAccessException(Guid EntityId, Guid cardId)
        : base($"You are not authorized to access card '{cardId}'.", EntityId) { }
}
