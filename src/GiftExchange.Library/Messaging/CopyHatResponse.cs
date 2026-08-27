namespace GiftExchange.Library.Messaging;

public record CopyHatResponse
{
    /// <summary>The new gift exchange. The source is left untouched.</summary>
    public required Guid HatId { get; init; }
}
