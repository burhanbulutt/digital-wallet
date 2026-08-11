using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Application.Interfaces.Infrastructure;
using System.Diagnostics;

namespace DigitalWallet.Application.Services;

public class CardService : ICardService
{
    private const int MaxActiveCardsPerHolder = 5;
    private const int MaxGenerationAttempts = 5;

    private readonly ICardGenerator _cardGenerator;
    private readonly ICardRepository _cardRepository;
    private readonly ICardHolderRepository _cardHolderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;

    public CardService(
        ICardGenerator cardGenerator,
        ICardRepository cardRepository,
        ICardHolderRepository cardHolderRepository,
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger)
    {
        _cardGenerator = cardGenerator;
        _cardRepository = cardRepository;
        _cardHolderRepository = cardHolderRepository;
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
    }

    public async Task<CardSecretsDto> CreateAsync(
        CardRequestDto request,
        CancellationToken ct = default)
    {

        // might delete this part when I implement jwt.
        if (!await _cardHolderRepository.ExistsAsync(request.CardHolderId, ct))
        {
            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Error,
                $"Card creation failed: card holder '{request.CardHolderId}' not found.", request.CardHolderId,
                ct: ct);

            //throw new CardHolderNotFoundException(request.CardHolderId);
            throw new Exception($"Card creation failed: card holder '{request.CardHolderId}' not found.");
        }

        var activeCount = await _cardRepository.CountActiveByHolderAsync(request.CardHolderId, ct);
        if (activeCount >= MaxActiveCardsPerHolder)
        {
            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Error,
                $"Card creation failed: holder '{request.CardHolderId}' already has "
              + $"{activeCount} active cards (limit {MaxActiveCardsPerHolder}).", request.CardHolderId,
                ct: ct);

            throw new CardLimitExceededException(request.CardHolderId, MaxActiveCardsPerHolder);
        }

        for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
        {
            var (card, cardNumber) = _cardGenerator.Generate(request.CardType, request.Brand);
            card.CardHolderId = request.CardHolderId;

            try
            {
                await _cardRepository.AddAsync(card, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DuplicateCardException)
            {
                //database is the authority on uniqueness, not a pre check!!
                if (attempt == MaxGenerationAttempts)
                {
                    await _processLogger.LogAsync(
                        ProcessName.CardCreation, LogLevel.Error,
                        $"Card creation failed after {MaxGenerationAttempts} collisions.",
                        request.CardHolderId,
                        ct: ct);
                    throw; 
                }

                continue;
            }

            await _processLogger.LogAsync(
                ProcessName.CardCreation, LogLevel.Success,
                $"Card created for holder '{request.CardHolderId}'.",
                card.Id, ct);

            return new CardSecretsDto(
                card.Id, cardNumber, card.ExpiryMonth, card.ExpiryYear,
                card.Brand, card.CardType, card.Status);
        }

        throw new UnreachableException();
    }
}