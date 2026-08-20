using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Application.DTOs.Transfers;
using DigitalWallet.Application.Interfaces.Infrastructure;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Domain.Services;

namespace DigitalWallet.Application.Services;

public class TransferService : ITransferService
{
    private const int MaxRetryAttempts = 3;

    private readonly ICardRepository _cardRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;
    private readonly TimeProvider _timeProvider;

    public TransferService(
        ICardRepository cardRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger,
        TimeProvider timeProvider)
    {
        _cardRepository = cardRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
        _timeProvider = timeProvider;
    }

    public async Task<TransferDto> CreateAsync(
        Guid fromCardId, Guid cardHolderId, string idempotencyKey,
        CreateTransferRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidTransferException(fromCardId, "an Idempotency-Key header is required.");

        // A retried request returns the original outcome instead of moving the
        // money again. 
        var existing = await _transferRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null)
            return existing;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var fromCard = await _cardRepository.GetTrackedForTransactionAsync(fromCardId, ct)
                               ?? throw new CardNotFoundException(fromCardId);

                if (fromCard.CardHolderId != cardHolderId)
                    throw new UnauthorizedCardAccessException(fromCardId);

                var toCard = await _cardRepository.GetTrackedForTransactionAsync(request.ToCardId, ct)
                             ?? throw new CardNotFoundException(request.ToCardId);

                var transfer = TransferPolicy.Execute(
                    fromCard, toCard, request.Amount, idempotencyKey, _timeProvider.GetUtcNow());

                await _transferRepository.AddAsync(transfer, ct);

                // Both balances and the transfer row commit together or not at all.
                await _unitOfWork.SaveChangesAsync(ct);

                await _processLogger.LogAsync(
                    ProcessName.TransferCreation, LogLevel.Success,
                    $"{request.Amount:N2} transferred from ****{fromCard.Last4} "
                  + $"to ****{toCard.Last4}.", transfer.Id);

                return TransferDto.From(transfer);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts)
            {
                await Task.Delay(Random.Shared.Next(20, 80), ct);
            }
            catch (ConcurrencyConflictException)
            {
                await LogFailureAsync(fromCardId, request.Amount, $"abandoned after {MaxRetryAttempts} concurrency conflicts");
                throw;// no Transfer row
            }
            // Access failures dont write Transfer row
            catch (CardNotFoundException)
            {
                await LogFailureAsync(fromCardId, request.Amount, "card not found");
                throw;
            }
            catch (UnauthorizedCardAccessException)
            {
                await LogFailureAsync(fromCardId, request.Amount, "not the caller's card");
                throw;
            }
            catch (OperationCanceledException)
            {
                // Everything here uses CancellationToken.None. the request token is already cancelled, so passing
                // it would fail before writing anything.

                // The token fired while awaiting the result, so the commit may or may not
                // have landed. (Case: cancellation fires after the command reaches SQL Server)
                var exist = await _transferRepository.GetByIdempotencyKeyAsync(
                    idempotencyKey, CancellationToken.None);

                if (exist is not null)
                {
                    // Commit landed. The money moved, so the audit says so. 
                    // Connection is closed, there is simply nobody left to return the DTO to.
                    var from = await _cardRepository.GetByIdAsync(fromCardId, CancellationToken.None)
                               ?? throw new CardNotFoundException(fromCardId);

                    await _processLogger.LogAsync(
                        ProcessName.TransferCreation, LogLevel.Success,
                        $"{request.Amount:N2} transferred from ****{from.Last4}; "
                      + "client disconnected before the response was sent.", exist.Id);
                }
                else
                {
                    await RecordCancelledTransferAsync(
                        fromCardId, request.ToCardId, request.Amount, idempotencyKey);
                }

                throw;
            }
            // Business failures do. the holder gets a record of why it did not go through.
            catch (DomainException ex)
            {
                await RecordFailedTransferAsync(
                    fromCardId, request.ToCardId, request.Amount, idempotencyKey, ex.Message);
                throw;
            }
        }
    }


    // Important: If I see a problem regarding those 2 "record non-completed entity" methods, 
    // using factory created context might be the solution
    // (fix: follow the same pattern as ProcessLogger)
    private async Task RecordCancelledTransferAsync(
        Guid fromCardId, Guid toCardId, decimal amount, string idempotencyKey)
    {
        // The cancelled SaveChanges may have left entities tracked. load bearing.
        _unitOfWork.Discard();

        var cancelled = TransferPolicy.Cancelled(
            fromCardId, toCardId, amount, idempotencyKey, _timeProvider.GetUtcNow());

        await _transferRepository.AddAsync(cancelled, CancellationToken.None);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        await LogFailureAsync(fromCardId, amount, "client disconnected before the transfer committed");
    }

    private async Task RecordFailedTransferAsync(
        Guid fromCardId, Guid toCardId, decimal amount, string idempotencyKey, string reason)
    {
        // every exception is already thrown before modifying any entity, not load bearing "Today". 
        // If one day, with a code refactor, a domain exception is thrown after the modification, this will be useful.
        _unitOfWork.Discard(); 

        var failed = TransferPolicy.Failed(
            fromCardId, toCardId, amount, idempotencyKey, reason, _timeProvider.GetUtcNow());

        await _transferRepository.AddAsync(failed);

        // Not cancellable. the record must land even if the caller has gone because exception has been catched.
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        await LogFailureAsync(fromCardId, amount, reason);
    }

    private Task LogFailureAsync(Guid fromCardId, decimal amount, string reason)
        => _processLogger.LogAsync(
            ProcessName.TransferCreation, LogLevel.Error,
             $"Transfer of {amount:N2} failed: {reason}", fromCardId);
}
