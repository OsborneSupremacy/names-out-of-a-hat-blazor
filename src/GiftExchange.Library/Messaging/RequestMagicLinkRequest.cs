namespace GiftExchange.Library.Messaging;

public record RequestMagicLinkRequest
{
    public required string Email { get; init; }
}
