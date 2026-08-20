using DigitalWallet.Application.DTOs.Transactions;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Services;

namespace DigitalWallet.Application.Services;

public class DebtPaymentService : IDebtPaymentService
{
    private const int MaxRetryAttempts = 3;

    private readonly ICardRepository _cardRepository;
    private readonly ICardTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;
    private readonly TimeProvider _timeProvider;

    public DebtPaymentService(
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

    public async Task<TransactionDto> PayDebtAsync(
        Guid debtCardId, Guid cardHolderId, string idempotencyKey,
        PayDebtRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidCardException(debtCardId, "an Idempotency-Key header is required.");

        if (debtCardId == request.SourceCardId)
            throw new InvalidCardException(debtCardId, "a card cannot pay off itself.");

        var replay = await _transactionRepository
            .GetByIdempotencyKeyAsync(idempotencyKey, debtCardId, ct);

        if (replay is not null)
            return replay;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Both re-read every attempt: two rows are written, so either
                // RowVersion can lose and the tracker gets cleared.
                var debtCard = await _cardRepository.GetTrackedForTransactionAsync(debtCardId, ct)
                               ?? throw new CardNotFoundException(debtCardId);

                var sourceCard = await _cardRepository.GetTrackedForTransactionAsync(request.SourceCardId, ct)
                                ?? throw new CardNotFoundException(request.SourceCardId);

                if (debtCard.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(debtCardId);

                if (sourceCard.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(request.SourceCardId);

                if (sourceCard.CardType != CardType.Debit)
                    throw new InvalidCardException(
                        sourceCard.Id, "debt can only be paid from a debit card.");

                if (debtCard.CardType == CardType.Debit)
                    throw new InvalidCardException(debtCard.Id, "a debit card carries no debt.");

                var budget = debtCard.GetRequiredBudget();
                var was80 = budget.WarningThreshold80;
                var was100 = budget.WarningThreshold100;

                var now = _timeProvider.GetUtcNow();

                // TODO: Do debt payments deserve a category.
                var incoming = TransactionPolicy.RecordLoad(
                    debtCard, request.Amount, Category.Diger,
                    $"Debt payment from card ****{sourceCard.Last4}", idempotencyKey, now);

                var outgoing = TransactionPolicy.RecordSpend(
                    sourceCard, request.Amount, Category.Diger,
                    $"Debt payment to card ****{debtCard.Last4}", idempotencyKey, now);

                await _transactionRepository.AddAsync(incoming, ct);
                await _transactionRepository.AddAsync(outgoing, ct);

                // Both balances and both rows commit together or not at all.
                await _unitOfWork.SaveChangesAsync(ct);

                await _processLogger.LogAsync(
                    ProcessName.TransactionCreation, LogLevel.Success,
                    $"{request.Amount:N2} paid from card ****{sourceCard.Last4} "
                  + $"toward card ****{debtCard.Last4}.", incoming.Id);

                return TransactionDto.From(outgoing);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts)
            {
                await Task.Delay(Random.Shared.Next(20, 80), ct);
            }
            catch (ConcurrencyConflictException)
            {
                await LogFailureAsync(debtCardId, request.Amount,
                    $"debt payment abandoned after {MaxRetryAttempts} concurrency conflicts");
                throw;
            }
            catch (Exception ex) when (ex is CardNotFoundException or UnauthorizedCardAccessException)
            {
                await LogFailureAsync(debtCardId, request.Amount, ex.Message);
                throw;
            }
            catch (OperationCanceledException)
            {
                var committed = await _transactionRepository
                    .GetByIdempotencyKeyAsync(idempotencyKey, debtCardId, CancellationToken.None);

                if (committed is not null)
                {
                    await _processLogger.LogAsync(
                        ProcessName.TransactionCreation, LogLevel.Success,
                        $"{request.Amount:N2} paid toward card {debtCardId}; "
                      + "client disconnected before the response was sent.", committed.Id);
                }
                else
                {
                    // Two rows, matching the two the operation would have written,
                    // so the history reads the same whatever the outcome.
                    await RecordUnsuccessfulPairAsync(debtCardId, request, idempotencyKey, reason: null);
                }

                throw;
            }
            catch (DomainException ex)
            {
                await RecordUnsuccessfulPairAsync(debtCardId, request, idempotencyKey, ex.Message);
                throw;
            }
        }
    }

    private async Task RecordUnsuccessfulPairAsync(
        Guid debtCardId, PayDebtRequest request, string idempotencyKey, string? reason)
    {
        _unitOfWork.Discard();

        var now = _timeProvider.GetUtcNow();

        // local function
        CardTransaction Row(Guid cardId, TransactionDirection direction) => reason is null
            ? TransactionPolicy.Cancelled(
                cardId, request.Amount, direction,
                Category.Diger, "Debt payment", idempotencyKey, now)
            : TransactionPolicy.Failed(
                cardId, request.Amount, direction,
                Category.Diger, "Debt payment", idempotencyKey, reason, now);

        await _transactionRepository.AddAsync(
            Row(debtCardId, TransactionDirection.Incoming), CancellationToken.None);

        await _transactionRepository.AddAsync(
            Row(request.SourceCardId, TransactionDirection.Outgoing), CancellationToken.None);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        await LogFailureAsync(debtCardId, request.Amount,
            reason ?? "client disconnected before the debt payment committed");
    }

    private Task LogFailureAsync(Guid cardId, decimal amount, string reason)
        => _processLogger.LogAsync(
            ProcessName.TransactionCreation, LogLevel.Error,
            $"Transaction of {amount:N2} failed: {reason}", cardId);
}