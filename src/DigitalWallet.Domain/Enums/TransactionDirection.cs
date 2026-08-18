namespace DigitalWallet.Domain.Enums;

// Amount is always positive; direction carries the sign.
// Outgoing: POS purchase, cash withdrawal.
// Incoming: POS refund, debt payment, top-up.
public enum TransactionDirection
{
    Incoming,
    Outgoing
}
