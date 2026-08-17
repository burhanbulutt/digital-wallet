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
        Guid cardId, Guid cardHolderId, CreateTransactionRequest request,
        CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Re-read on every attempt. A conflict clears the change tracker,
                // so retrying with the previous entities would use stale RowVersions.
                var card = await _cardRepository.GetTrackedForSpendAsync(cardId, ct)
                           ?? throw new CardNotFoundException(cardId);

                if (card.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(cardId);

                // Captured before the spend. Because it is only way to know threshold was crossed by this
                var budget = card.Budget;
                var was80 = budget?.WarningThreshold80 ?? false;
                var was100 = budget?.WarningThreshold100 ?? false;

                var transaction = TransactionPolicy.RecordSpend(
                    card, request.Amount, request.Category, request.Description,
                    _timeProvider.GetUtcNow());

                // One SaveChanges covers the transaction row plus Card.Balance or
                // Budget.SpentAmount, so EF wraps them in a single transaction.
                await _transactionRepository.AddAsync(transaction, ct);

                await _unitOfWork.SaveChangesAsync(ct);

                await _processLogger.LogAsync(
                    ProcessName.TransactionCreation, LogLevel.Success,
                    $"{request.Amount:N2} spent on card ****{card.Last4}.", transaction.Id);

                await LogThresholdCrossingAsync(card.Id, card.Last4, budget, was80, was100, ct);

                return TransactionDto.From(transaction);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts)
            {
                // Jitter, so two colliding requests don't retry in lockstep.
                await Task.Delay(Random.Shared.Next(20, 80), ct);
            }
            catch (ConcurrencyConflictException)
            {
                await _processLogger.LogAsync(
                    ProcessName.TransactionCreation, LogLevel.Error,
                    $"Spend abandoned after {MaxRetryAttempts} concurrency conflicts.", cardId);
                throw;
            }
            catch (DomainException ex)
            {
                await _processLogger.LogAsync(
                    ProcessName.TransactionCreation, LogLevel.Error,
                    $"Spend rejected: {ex.Message}", cardId);
                throw;
            }
        }
    }

    public Task<PagedResult<TransactionDto>> GetPagedAsync(
        Guid cardId, Guid cardHolderId, TransactionListFilter filter,
        PaginationQuery pagination, CancellationToken ct = default)
        => _transactionRepository.GetPagedForCardAsync(cardId, cardHolderId, filter, pagination, ct);

    private async Task LogThresholdCrossingAsync(
        Guid cardId, string last4, Budget? budget,
        bool was80, bool was100, CancellationToken ct)
    {
        if (budget is null) return;

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
}
