namespace GiftExchange.Library.Messaging;

public record GetHatsResponse
{
    public required ImmutableList<HatMetaData> Hats { get; init; }
}
