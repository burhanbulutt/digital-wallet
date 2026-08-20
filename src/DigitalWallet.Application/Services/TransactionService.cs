using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Services;

namespace DigitalWallet.Application.Services;

public class TransactionService : ITransactionService
{
    private const int MaxRetryAttempts = 3;

    private readonly ICardRepository _cardRepository;
    private readonly ICardTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;
    private readonly TimeProvider _timeProvider;

    public TransactionService(
        ICardRepository cardRepository,
        ICardTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger,
        TimeProvider timeProvider)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
        _timeProvider = timeProvider;
    }

    public async Task<TransactionDto> AddAsync(
        Guid cardId, Guid cardHolderId, string idempotencyKey,
        CreateTransactionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidCardException(cardId, "an Idempotency-Key header is required.");

        // A retried request returns the original outcome instead of charging twice.
        var existing = await _transactionRepository
            .GetByIdempotencyKeyAsync(idempotencyKey, cardId, ct);

        if (existing is not null)
            return existing;

        for (var attempt = 1; ; attempt++)
        {
            Card? card = null;
            Budget? budget = null;
            bool was80 = false, was100 = false;

            try
            {
                // Re-read on every attempt. A conflict clears the change tracker,
                // so retrying with the previous entities would use stale RowVersions.
                card = await _cardRepository.GetTrackedForTransactionAsync(cardId, ct)
                           ?? throw new CardNotFoundException(cardId);

                if (card.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(cardId);

                // Captured before the spend. Because it is only way to know threshold was crossed by this
                budget = card.Budget;
                was80 = budget?.WarningThreshold80 ?? false;
                was100 = budget?.WarningThreshold100 ?? false;

                var now = _timeProvider.GetUtcNow();

                var transaction = request.Direction == TransactionDirection.Incoming
                    ? TransactionPolicy.RecordLoad(
                        card, request.Amount, request.Category, request.Description, idempotencyKey, now)
                    : TransactionPolicy.RecordSpend(
                        card, request.Amount, request.Category, request.Description, idempotencyKey, now);

                // One SaveChanges covers the transaction row plus Card.Balance or
                // Budget.SpentAmount, so EF wraps them in a single transaction.
                await _transactionRepository.AddAsync(transaction, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                await _processLogger.LogAsync(
                    ProcessName.TransactionCreation, LogLevel.Success,
                    $"{request.Direction} transaction of {request.Amount:N2} on card ****{card.Last4}.",
                    transaction.Id);

                await LogThresholdCrossingAsync(card.Id, card.Last4, budget, was80, was100);

                return TransactionDto.From(transaction);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts)
            {
                // Jitter, so two colliding requests don't retry in lockstep.
                await Task.Delay(Random.Shared.Next(20, 80), ct);
            }
            // no row, so the key stays usable and a retry can succeed.
            catch (ConcurrencyConflictException)
            {
                await LogFailureAsync(cardId, request.Amount,
                    $"abandoned after {MaxRetryAttempts} concurrency conflicts");
                throw;
            }
            // Access failures write no row.
            catch (Exception ex) when (ex is CardNotFoundException or UnauthorizedCardAccessException)
            {
                await LogFailureAsync(cardId, request.Amount, ex.Message);
                throw;
            }
            catch (OperationCanceledException)
            {
                // Everything here uses CancellationToken.None. the request token is already cancelled, so passing
                // it would fail before writing anything.

                // The token fired while awaiting the result, so the commit may or may not
                // have landed. (Case: cancellation fires after the command reaches SQL Server)
                var committed = await _transactionRepository
                    .GetByIdempotencyKeyAsync(idempotencyKey, cardId, CancellationToken.None);

                if (committed is not null)
                {
                    await LogThresholdCrossingAsync(card!.Id, card.Last4, budget, was80, was100);
                    // It landed. The money moved.
                    await _processLogger.LogAsync(
                        ProcessName.TransactionCreation, LogLevel.Success,
                        $"{request.Amount:N2} recorded on card {cardId}; "
                      + "client disconnected before the response was sent.", committed.Id);
                }
                else
                {
                    await RecordUnsuccessfulAsync(cardId, request, idempotencyKey, reason: null);
                }

                throw;
            }
            catch (DomainException ex)
            {
                await RecordUnsuccessfulAsync(cardId, request, idempotencyKey, ex.Message);
                throw;
            }
        }
    }

    // A null reason means the client disconnected before the commit landed;
    // anything else is a business rejection.
    private async Task RecordUnsuccessfulAsync(
        Guid cardId, CreateTransactionRequest request, string idempotencyKey, string? reason)
    {
        // Load bearing for cancelled transactions.
        _unitOfWork.Discard();

        var now = _timeProvider.GetUtcNow();

        var row = reason is null
            ? TransactionPolicy.Cancelled(
                cardId, request.Amount, request.Direction,
                request.Category, request.Description, idempotencyKey, now)
            : TransactionPolicy.Failed(
                cardId, request.Amount, request.Direction,
                request.Category, request.Description, idempotencyKey, reason, now);

        await _transactionRepository.AddAsync(row, CancellationToken.None);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        await LogFailureAsync(cardId, request.Amount,
            reason ?? "client disconnected before the transaction committed");
    }

    private async Task LogThresholdCrossingAsync(
        Guid cardId, string last4, Budget? budget, bool was80, bool was100)
    {
        if (budget is null) return;

        // false before, true after transaction => log
        if (!was100 && budget.WarningThreshold100)
        {
            await _processLogger.LogAsync(
                ProcessName.BudgetWarning, LogLevel.Warn,
                $"Card ****{last4} reached 100% of its {budget.LimitAmount:N2} limit "
              + $"({budget.SpentAmount:N2} spent).", cardId);
        }
        else if (!was80 && budget.WarningThreshold80)
        {
            await _processLogger.LogAsync(
                ProcessName.BudgetWarning, LogLevel.Warn,
                $"Card ****{last4} reached 80% of its {budget.LimitAmount:N2} limit "
              + $"({budget.SpentAmount:N2} spent).", cardId);
        }
    }

    private Task LogFailureAsync(Guid cardId, decimal amount, string reason)
        => _processLogger.LogAsync(
            ProcessName.TransactionCreation, LogLevel.Error,
            $"Transaction of {amount:N2} failed: {reason}", cardId);
}
