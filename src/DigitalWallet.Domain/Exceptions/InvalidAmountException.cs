namespace DigitalWallet.Domain.Exceptions;

public class InvalidAmountException : DomainException
{
    public InvalidAmountException(Guid entityId, decimal amount)
        : base($"Amount must be greater than zero (received {amount:N2}).", entityId) { }

    public InvalidAmountException(decimal amount)
        : base($"Amount must be greater than zero (received {amount:N2}).") { }
}
