using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Services;
using DigitalWallet.Application.Interfaces.Infrastructure;
using System.Diagnostics;
using DigitalWallet.Application.DTOs.Common;

namespace DigitalWallet.Application.Services;

public class CardService : ICardService
{
    private const int MaxGenerationAttempts = 5;

    private readonly ICardGenerator _cardGenerator;
    private readonly ICardRepository _cardRepository;
    private readonly ICardHolderRepository _cardHolderRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;

    public CardService(
        ICardGenerator cardGenerator,
        ICardRepository cardRepository,
        ICardHolderRepository cardHolderRepository,
        IBudgetRepository budgetRepository,
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger)
    {
        _cardGenerator = cardGenerator;
        _cardRepository = cardRepository;
        _cardHolderRepository = cardHolderRepository;
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
    }

    public async Task<CardSecretsDto> CreateAsync(
        CardRequestDto request,
        CancellationToken ct = default)
    {
        // this also answers "does this holder exist".
        var salary = await _cardHolderRepository.GetSalaryAsync(request.CardHolderId, ct);
        if (salary is null)
        {
            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Error,
                $"Card creation failed: card holder '{request.CardHolderId}' not found.", request.CardHolderId);

            throw new CardHolderNotFoundException(request.CardHolderId);
            //throw new Exception($"Card creation failed: card holder '{request.CardHolderId}' not found.");
        }

        var activeCount = await _cardRepository.CountActiveByHolderAsync(request.CardHolderId, ct);
        try
        {
            CardPolicy.EnsureCanIssueCard(request.CardHolderId, activeCount);
        }
        catch (CardLimitExceededException)
        {
            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Error,
                $"Card creation failed: holder '{request.CardHolderId}' already has "
              + $"{activeCount} active cards (limit {CardPolicy.MaxActiveCardsPerHolder}).",
                request.CardHolderId);

            throw;
        }
        
        // I didn't do the all of try-catch blockes since there will be exception middleware.
        CardPolicy.EnsureMainCardShape(request.CardType, request.MainCardId);

        for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
        {
            // DuplicateCardNumber is checked via UnitOfWork, thus need complete context.
            Budget? parentBudget = null;
            var limitAmount = 0m;

            switch (request.CardType)
            {
                case CardType.Debit:
                    break;

                case CardType.Credit:
                    var allocated = await _budgetRepository
                        .SumCreditLimitsByHolderAsync(request.CardHolderId, ct);

                    limitAmount = CreditLimitPolicy.ResolveRequestedLimit(
                        request.RequestedLimit,
                        CreditLimitPolicy.AvailableToAllocate(salary.Value, allocated));
                    break;

                case CardType.Virtual:
                    parentBudget = await LoadParentBudgetAsync(request, ct);
                    limitAmount = CreditLimitPolicy.ResolveRequestedLimit(
                        request.RequestedLimit,
                        BudgetPolicy.Available(parentBudget));
                    break;
            }

            var (card, cardNumber) = _cardGenerator.Generate(request.CardType, request.Brand);

            card.CardHolderId = request.CardHolderId;
            card.MainCardId = request.MainCardId;

            var budget = request.CardType switch
            {
                CardType.Credit  => BudgetPolicy.AllocateForCreditCard(card, limitAmount),
                CardType.Virtual => BudgetPolicy.AllocateForVirtualCard(card, parentBudget!, limitAmount),
                _ => null   // Debit spends from Balance and has no allocation.
            };

            try
            {
                // The Budget rides along via card.Budget, so one Add covers both.
                await _cardRepository.AddAsync(card, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DuplicateCardException)
            {
                // database is the authority on uniqueness, not a pre check!!
                if (attempt == MaxGenerationAttempts)
                {
                    await _processLogger.LogAsync(
                        ProcessName.CardCreation, LogLevel.Error,
                        $"Card creation failed after {MaxGenerationAttempts} collisions.",
                        request.CardHolderId);
                    throw;
                }

                continue;
            }

            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Success,
                $"{request.CardType} card created for holder '{request.CardHolderId}'.",
                card.Id);

            return new CardSecretsDto(
                card.Id, cardNumber, card.ExpiryMonth, card.ExpiryYear,
                card.Brand, card.CardType, card.Status,
                budget?.LimitAmount, card.MainCardId);
        }

        throw new UnreachableException();
    }

    public Task<PagedResult<CardDto>> GetPagedAsync(
        Guid cardHolderId, CardListFilter filter, PaginationQuery pagination,
        CancellationToken ct = default)
        // ownership check is done with WHERE clause
        => _cardRepository.GetPagedForHolderAsync(cardHolderId, filter, pagination, ct);
    
    public async Task<CardDto> GetByIdAsync(Guid id, Guid cardHolderId, CancellationToken ct = default)
    {
        var (Dto, OwnerId) = await _cardRepository.GetDtoWithOwnerAsync(id, ct)
                 ?? throw new CardNotFoundException(id);

        if (OwnerId != cardHolderId)
            throw new UnauthorizedCardAccessException(id);

        return Dto;
    }
    
    public async Task<CardDto> UpdateStatusAsync(
        Guid id, Guid cardHolderId, CardStatus newStatus, CancellationToken ct = default)
    {
        var card = await _cardRepository.GetTrackedForStatusChangeAsync(id, ct)
                   ?? throw new CardNotFoundException(id);
    
        if (card.CardHolderId != cardHolderId)
            throw new UnauthorizedCardAccessException(id);
    
        var previous = card.Status;

        if (newStatus == CardStatus.Closed)
            CardPolicy.Close(card);
        else
            CardPolicy.TransitionTo(card, newStatus);

        await _unitOfWork.SaveChangesAsync(ct);
    
        await _processLogger.LogAsync(
            ProcessName.CardStatusUpdate, LogLevel.Success,
            $"Card status changed from {previous} to {newStatus}.", card.Id);

        if (newStatus == CardStatus.Closed && card.CardType == CardType.Debit && card.Balance > 0m)
            await _processLogger.LogAsync(
                ProcessName.CardStatusUpdate, LogLevel.Warn,
                $"Debit card closed holding {card.Balance:N2}.", card.Id);
    
        return CardDto.From(card);
    }

    private async Task<Budget> LoadParentBudgetAsync(CardRequestDto request, CancellationToken ct)
    {
        // Scoped to the holder, so a virtual card can never be hung off someone
        // else's credit limit.
        var parent = await _cardRepository.GetTrackedByIdForHolderAsync(
                         request.MainCardId!.Value, request.CardHolderId, ct)
                     ?? throw new InvalidMainCardException("Main card not found for this holder.");

        if (parent.CardType != CardType.Credit)
            throw new InvalidMainCardException(parent.Id, "Main card must be a credit card.");

        // status cannot be closed, GetTrackedByIdForHolderAsync makes sure of it.
        if (parent.Status != CardStatus.Active)
            throw new CardStateConflictException(parent.Id, "Main card must be active.");

        return parent.Budget
               ?? throw new InvalidMainCardException(parent.Id, "Main card has no budget.");
    }
    
}
