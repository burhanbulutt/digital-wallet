namespace DigitalWallet.Domain.Exceptions;

public class BudgetExceededException : DomainException
{
    public BudgetExceededException(Guid entityId)
        : base("This transaction would exceed the budget limit.", entityId) { }

    public BudgetExceededException(Guid entityId, decimal limit, decimal currentSpent, decimal transactionAmount)
        : base($"Budget exceeded. Limit: {limit:C}, Already spent: {currentSpent:C}, Transaction: {transactionAmount:C}", entityId) { }
}
