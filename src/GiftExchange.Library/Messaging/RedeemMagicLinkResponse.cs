namespace GiftExchange.Library.Messaging;

public record RedeemMagicLinkResponse
{
    public required string AccessToken { get; init; }

    public required string Email { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
