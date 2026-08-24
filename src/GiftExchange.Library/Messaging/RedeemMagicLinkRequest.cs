namespace GiftExchange.Library.Messaging;

public record RedeemMagicLinkRequest
{
    public required string Token { get; init; }
}
