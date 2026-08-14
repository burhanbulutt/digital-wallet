// Application/DTOs/Cards/UpdateCardStatusRequest.cs
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.DTOs.Cards;

public record UpdateCardStatusRequest(CardStatus Status);