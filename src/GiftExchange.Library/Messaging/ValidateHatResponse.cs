namespace GiftExchange.Library.Messaging;

// ReSharper disable once ClassNeverInstantiated.Global
public record ValidateHatResponse
{
    public required bool Success { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
