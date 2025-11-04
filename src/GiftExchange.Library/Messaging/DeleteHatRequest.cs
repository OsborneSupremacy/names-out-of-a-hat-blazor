namespace GiftExchange.Library.Messaging;

public record DeleteHatRequest
{
    public required Guid HatId { get; init; }
}
