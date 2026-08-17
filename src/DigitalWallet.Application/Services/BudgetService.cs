using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Services;

namespace DigitalWallet.Application.Services;

public class BudgetService : IBudgetService
{
    private const int MaxRetryAttempts = 3;

    private readonly ICardRepository _cardRepository;
    private readonly ICardHolderRepository _cardHolderRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;

    public BudgetService(
        ICardRepository cardRepository,
        ICardHolderRepository cardHolderRepository,
        IBudgetRepository budgetRepository,
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger)
    {
        _cardRepository = cardRepository;
        _cardHolderRepository = cardHolderRepository;
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
    }

    public async Task<CardDto> UpdateLimitAsync(
        Guid cardId, Guid cardHolderId, decimal newLimit, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Re-read every attempt: a virtual card's change writes the parent's
                // budget too, so a conflict there clears the tracker.
                var card = await _cardRepository.GetTrackedForLimitChangeAsync(cardId, ct)
                           ?? throw new CardNotFoundException(cardId);

                if (card.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(cardId);

                if (card.Status == CardStatus.Closed)
                    throw new InvalidCardException(cardId, "a closed card's limit cannot be changed.");

                var budget = CardPolicy.GetRequiredBudget(card);

                var previousLimit = budget.LimitAmount;

                switch (card.CardType)
                {
                    case CardType.Credit:
                        await ApplyCreditLimitAsync(card, budget, cardHolderId, newLimit, ct);
                        break;

                    case CardType.Virtual:
                        var parentBudget = card.MainCard?.Budget
                            ?? throw new InvalidMainCardException(
                                cardId, "main card budget was not loaded.");

                        BudgetPolicy.ChangeVirtualCardLimit(budget, parentBudget, newLimit);
                        break;

                    default:
                        throw new InvalidCardException(
                            cardId, $"a {card.CardType} card has no limit to change.");// just in case.
                }

                await _unitOfWork.SaveChangesAsync(ct);

                await _processLogger.LogAsync(
                    ProcessName.BudgetUpdate, LogLevel.Success,
                    $"Card ****{card.Last4} limit changed from {previousLimit:N2} to {newLimit:N2}.",
                    cardId, ct);

                return CardDto.From(card);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts)
            {
                await Task.Delay(Random.Shared.Next(20, 80), ct);
            }
            catch (ConcurrencyConflictException)
            {
                await _processLogger.LogAsync(
                    ProcessName.BudgetUpdate, LogLevel.Error,
                    $"Limit change abandoned after {MaxRetryAttempts} concurrency conflicts.",
                    cardId, ct);
                throw;
            }
            catch (DomainException ex)
            {
                await _processLogger.LogAsync(
                    ProcessName.BudgetUpdate, LogLevel.Error,
                    $"Limit change rejected: {ex.Message}", cardId, ct);
                throw;
            }
        }
    }

    // The new limit is checked against the salary ceiling with this card taken
    // out of the allocated sum
    private async Task ApplyCreditLimitAsync(
        Card card, Budget budget,
        Guid cardHolderId, decimal newLimit, CancellationToken ct)
    {
        var salary = await _cardHolderRepository.GetSalaryAsync(cardHolderId, ct)
                     ?? throw new CardHolderNotFoundException(cardHolderId);

        var totalAllocated = await _budgetRepository
            .SumCreditLimitsByHolderAsync(cardHolderId, ct);

        // Subtracting the current card's limit from total to decide if the new limit is acceptable.
        var allocatedElsewhere = totalAllocated - budget.LimitAmount;

        var ceiling = CreditLimitPolicy.AvailableToAllocate(salary, allocatedElsewhere);

        if (newLimit > ceiling)
            throw new CreditLimitExceededException(card.Id, newLimit, ceiling);

        // Rejects anything below SpentAmount + ReservedAmount
        BudgetPolicy.ChangeLimit(budget, newLimit);
    }
}
