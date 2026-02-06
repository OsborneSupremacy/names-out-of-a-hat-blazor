namespace GiftExchange.Library.Messaging;

public record HatMetaData
{
    public required Guid HatId { get; init; }

    public required string HatName { get; init; }
}
