namespace DigitalWallet.Domain.Exceptions;

public class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException(Guid EntityId)
        : base("Insufficient balance to complete this operation.", EntityId) { }

    public InsufficientBalanceException(Guid entityId, decimal requested, decimal available)
        : base($"Insufficient balance. Requested: {requested:C}, Available: {available:C}", entityId) { }
}
