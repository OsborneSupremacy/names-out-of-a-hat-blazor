namespace GiftExchange.Library.Messaging;

public record DeleteHatResponse
{
    public required string SuccessMessage { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
