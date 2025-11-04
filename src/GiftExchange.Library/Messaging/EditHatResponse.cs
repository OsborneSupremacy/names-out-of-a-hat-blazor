namespace GiftExchange.Library.Messaging;

public record EditHatResponse
{
    public required string SuccessMessage { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
