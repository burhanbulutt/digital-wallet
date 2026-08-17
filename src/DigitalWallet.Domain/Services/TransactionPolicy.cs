using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Services;

// A POS spend. One direction only, and completely separate from Transfer.
public static class TransactionPolicy
{
    // The money moves and the record is created in one call, so a balance can
    // never change without a matching row.
    public static CardTransaction RecordSpend(
        Card card, decimal amount, Category category, string? description, DateTimeOffset when)
    {
        CardPolicy.Spend(card, amount);

        return new CardTransaction
        {
            Card = card, 
            Amount = amount,
            Category = category,
            Description = description,
            TransactionDate = when
        };
    }
}
