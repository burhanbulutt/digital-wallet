namespace DigitalWallet.Domain.Exceptions;

public class CardHolderNotFoundException : DomainException
{
    public CardHolderNotFoundException(Guid EntityId)
        : base("Card holder not found.", EntityId) { }

    public CardHolderNotFoundException()
        : base("Card holder  not found.") { }
}