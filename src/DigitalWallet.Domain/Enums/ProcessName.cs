namespace DigitalWallet.Domain.Enums;

public enum ProcessName
{

    // Might change in the future(addition or deletion)
    //Auth
    Registration,
    Login,

    //Card
    CardCreation,
    CardRetrieval,
    CardListing,
    CardStatusUpdate,

    //Transaction
    TransactionCreation,
    TransactionListing,

    //Budget
    BudgetCreation,
    BudgetRetrieval,
    BudgetUpdate,
    BudgetWarning,

    //Transfer
    TransferCreation,
    TransferListing,

    //Summary
    SummaryRetrieval
}
