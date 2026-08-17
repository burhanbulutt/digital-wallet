namespace DigitalWallet.Domain.Exceptions;

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException()
        : base("The record was modified by another operation. Please retry.") { }
}