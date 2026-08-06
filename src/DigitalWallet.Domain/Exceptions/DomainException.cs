namespace DigitalWallet.Domain.Exceptions;
public abstract class DomainException : Exception
{
    public Guid? EntityId { get; }

    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Guid entityId) : base(message)
    {
        EntityId = entityId;
    }
}