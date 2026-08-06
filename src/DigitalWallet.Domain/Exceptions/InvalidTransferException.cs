namespace DigitalWallet.Domain.Exceptions;

public class InvalidTransferException : DomainException
{
    public InvalidTransferException(Guid EntityId)
        : base("The transfer is not valid.", EntityId) { }

    public InvalidTransferException(Guid EntityId, string reason)
        : base($"Invalid transfer: {reason}", EntityId) { }
}
