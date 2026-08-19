using System.Linq.Expressions;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Transfers;

// Both cards are named, so the client can work out direction relative to
// whichever card it is showing. A transfer between two of the holder's own
// cards is outgoing on one and incoming on the other.
public record TransferDto(
    Guid Id,
    Guid FromCardId,
    string FromCardLast4,
    Guid ToCardId,
    string ToCardLast4,
    decimal Amount,
    TransferStatus Status,
    string? FailureReason,
    DateTimeOffset TransferDate)
{
    public static Expression<Func<Transfer, TransferDto>> Projection => t =>
        new TransferDto(
            t.Id,
            t.FromCardId, t.FromCard.Last4,
            t.ToCardId, t.ToCard.Last4,
            t.Amount, t.Status, t.FailureReason, t.TransferDate);

    private static readonly Func<Transfer, TransferDto> Compiled = Projection.Compile();

    // Requires FromCard and ToCard to be loaded.
    public static TransferDto From(Transfer transfer) => Compiled(transfer);
}
