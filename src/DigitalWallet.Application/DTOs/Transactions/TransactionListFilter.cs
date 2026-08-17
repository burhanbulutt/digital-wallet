using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Transactions;

// To is exclusive, so ?to=2026-09-01 means all of August without an off-by-one
// on the last day.
public record TransactionListFilter(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Category? Category = null);
