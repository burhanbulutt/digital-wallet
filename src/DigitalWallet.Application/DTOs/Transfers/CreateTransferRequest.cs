namespace DigitalWallet.Application.DTOs.Transfers;

// IdempotencyKey comes from the Idempotency-Key header, not the body — the
// client generates it once and reuses it across retries of the same intent.
public record CreateTransferRequest(Guid ToCardId, decimal Amount);
