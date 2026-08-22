namespace DigitalWallet.Domain.Exceptions;

// thrown by UnitOfWork
public class UniqueViolationException : DomainException
{
    public UniqueViolationException()
        : base("A record with the same value already exists.") { }
}
