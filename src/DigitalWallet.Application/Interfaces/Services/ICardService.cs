using DigitalWallet.Application.DTOs.Cards;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ICardService
{
    Task<CardSecretsDto> CreateAsync(CardRequestDto request, CancellationToken ct = default);
}