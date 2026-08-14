using DigitalWallet.Application.DTOs.Cards;
using DigitalWallet.Application.DTOs.Common;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ICardService
{
    Task<CardSecretsDto> CreateAsync(CardRequestDto request, CancellationToken ct = default);
    
    Task<PagedResult<CardDto>> GetPagedAsync(
        Guid cardHolderId, CardListFilter filter, PaginationQuery pagination, CancellationToken ct = default);

    Task<CardDto> GetByIdAsync(Guid id, Guid cardHolderId, CancellationToken ct = default);

    Task<CardDto> UpdateStatusAsync(
        Guid id, Guid cardHolderId, CardStatus newStatus, CancellationToken ct = default);
}