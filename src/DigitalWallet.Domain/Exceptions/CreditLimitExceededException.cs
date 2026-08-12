namespace DigitalWallet.Domain.Exceptions;

public class CreditLimitExceededException : DomainException
{
    public decimal Requested { get; }
    public decimal Available { get; }

    public CreditLimitExceededException(Guid entityId, decimal requested, decimal available)
        : base($"Requested amount {requested:N2} exceeds the available limit of {available:N2}.", entityId)
    {
        Requested = requested;
        Available = available;
    }

    public CreditLimitExceededException(decimal requested, decimal available)
        : base($"Requested amount {requested:N2} exceeds the available limit of {available:N2}.")
    {
        Requested = requested;
        Available = available;
    }
}
