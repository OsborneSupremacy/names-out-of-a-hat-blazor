namespace GiftExchange.Library.Messaging;

public record ValidateHatResponse
{
    public required bool Success { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
